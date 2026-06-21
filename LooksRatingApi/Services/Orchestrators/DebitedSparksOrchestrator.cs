using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using LooksRatingApi.Services.SparksLedger;
using LooksRatingApi.Services.SparksWallet;
using LooksRatingGrpc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LooksRatingApi.Services.Orchestrators
{
    public class DebitedSparksOrchestrator : IDebitedSparksOrchestrator
    {
        private readonly ICurrencyDebitedService _currencyDebitedService;
        private readonly ILogger<DebitedSparksOrchestrator> _logger;
        private readonly LooksRatingDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly ISparksLedgerRepository _sparksLedgerRepository;
        private readonly ISparksDebitIdempotencyRepository _sparksDebitIdempotencyRepository;
        private readonly IWritingOffSparksRepository _writingOffSparksRepository;
        private readonly ISparksOrphanDebitResolver _sparksOrphanDebitResolver;

        public DebitedSparksOrchestrator(
            ICurrencyDebitedService currencyDebitedService,
            ILogger<DebitedSparksOrchestrator> logger,
            LooksRatingDbContext context,
            IUserRepository userRepository,
            ISparksLedgerRepository sparksLedgerRepository,
            ISparksDebitIdempotencyRepository sparksDebitIdempotencyRepository,
            IWritingOffSparksRepository writingOffSparksRepository,
            ISparksOrphanDebitResolver sparksOrphanDebitResolver)
        {
            _currencyDebitedService = currencyDebitedService;
            _logger = logger;
            _context = context;
            _userRepository = userRepository;
            _sparksLedgerRepository = sparksLedgerRepository;
            _sparksDebitIdempotencyRepository = sparksDebitIdempotencyRepository;
            _writingOffSparksRepository = writingOffSparksRepository;
            _sparksOrphanDebitResolver = sparksOrphanDebitResolver;
        }

        public async Task<Result<DebitedSparksResponse>> DebitedSparks(
            long telegramId,
            int starsCount,
            string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByTelegramId(telegramId);
            if (user == null)
            {
                return Result.Success(new DebitedSparksResponse { Success = false, Message = "Пользователь не найден" });
            }

            if (user.Status == Enums.VipStatus.Unavaillable)
            {
                return Result.Success(new DebitedSparksResponse
                {
                    Success = false,
                    Message = "Для начала нужно приобрести вип статус",
                });
            }

            if (!SparksGiftExchangeRules.TryGetSparksCost(starsCount, out var sparks))
            {
                return Result.Success(new DebitedSparksResponse
                {
                    Success = false,
                    Message = "Недопустимая стоимость подарка",
                });
            }

            string? normalizedKey = null;
            SparksDebitIdempotency? existingDebit = null;
            if (idempotencyKey is not null)
            {
                if (!IdempotencyKeyService.TryNormalizeClientKey(idempotencyKey, out var key))
                {
                    return Result.Success(new DebitedSparksResponse
                    {
                        Success = false,
                        Message = "Ключ идемпотентности не указан",
                    });
                }

                normalizedKey = key;

                existingDebit = await _sparksDebitIdempotencyRepository.GetByUserIdAndIdempotencyKey(
                    user.Id,
                    normalizedKey);

                var existingWritingOff = await _writingOffSparksRepository.GetByUserIdAndIdempotencyKey(
                    user.Id,
                    normalizedKey);

                if (existingDebit is not null
                    && existingDebit.CompensatedAt is null
                    && (existingWritingOff is null || IsActiveWritingOff(existingWritingOff)))
                {
                    if (existingDebit.StarsCount != starsCount)
                    {
                        return Result.Success(new DebitedSparksResponse
                        {
                            Success = false,
                            Message = "Недопустимая стоимость подарка",
                        });
                    }

                    return Result.Success(new DebitedSparksResponse
                    {
                        Success = true,
                        Message = "Списание уже выполнено",
                    });
                }

                if (existingWritingOff is not null && IsActiveWritingOff(existingWritingOff))
                {
                    return Result.Success(new DebitedSparksResponse
                    {
                        Success = true,
                        Message = "Списание уже выполнено",
                    });
                }
            }

            try
            {
                await _sparksOrphanDebitResolver.ResolveOrphansAsync(
                    user.Id,
                    normalizedKey,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orphan debit resolution failed for telegramId={TelegramId}", telegramId);
                return Result.Success(new DebitedSparksResponse
                {
                    Success = false,
                    Message = "Не удалось списать искры",
                });
            }

            var wallet = await _sparksLedgerRepository.GetSparksByUserId(user.Id, cancellationToken);
            if (wallet is null)
            {
                return Result.Success(new DebitedSparksResponse
                {
                    Success = false,
                    Message = "Кошелёк искр не найден",
                });
            }

            var balance = await _sparksLedgerRepository.GetBalanceAsync(user.Id, cancellationToken);
            if (balance - sparks < 0)
            {
                return Result.Success(new DebitedSparksResponse
                {
                    Success = false,
                    Message = "Недостаточно искр на балансе",
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var debitEventId = await _currencyDebitedService.Debited(user.Id, sparks, cancellationToken);

                if (normalizedKey is not null)
                {
                    var idempotencyRecord = await _sparksDebitIdempotencyRepository.GetByUserIdAndIdempotencyKey(
                        user.Id,
                        normalizedKey) ?? existingDebit;
                    if (idempotencyRecord is not null)
                    {
                        idempotencyRecord.RenewAfterDebit(debitEventId, sparks, starsCount);
                    }
                    else
                    {
                        var newRecord = SparksDebitIdempotency.Create(
                            user.Id,
                            normalizedKey,
                            debitEventId,
                            sparks,
                            starsCount);
                        if (newRecord.IsFailure)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return Result.Success(new DebitedSparksResponse
                            {
                                Success = false,
                                Message = "Ключ идемпотентности не указан",
                            });
                        }

                        await _sparksDebitIdempotencyRepository.Add(newRecord.Value);
                    }
                }

                await _sparksLedgerRepository.SaveChangesAsync(cancellationToken);
                await _sparksDebitIdempotencyRepository.SaveChangesAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (normalizedKey is not null && IsDuplicateIdempotencyKey(ex))
            {
                await transaction.RollbackAsync(cancellationToken);
                var duplicate = await _sparksDebitIdempotencyRepository.GetByUserIdAndIdempotencyKey(
                    user.Id,
                    normalizedKey);
                if (duplicate is not null)
                {
                    return Result.Success(new DebitedSparksResponse
                    {
                        Success = true,
                        Message = "Списание уже выполнено",
                    });
                }

                throw;
            }
            catch (SparksLedgerOperationException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex, "Sparks debit failed for telegramId={TelegramId}", telegramId);
                return Result.Success(new DebitedSparksResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Sparks debit failed for telegramId={TelegramId}", telegramId);
                return Result.Success(new DebitedSparksResponse
                {
                    Success = false,
                    Message = "Не удалось списать искры",
                });
            }

            return Result.Success(new DebitedSparksResponse { Success = true, Message = "Успешно" });
        }

        private static bool IsDuplicateIdempotencyKey(DbUpdateException exception) =>
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

        private static bool IsActiveWritingOff(WritingOffSparks writingOff) =>
            writingOff.Status is Enums.OutputStatusEnum.Pending or Enums.OutputStatusEnum.Confirmed;
    }
}
