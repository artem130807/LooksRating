using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Services;
using LooksRatingApi.Services.SparksWallet;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.Orchestrators
{
    public sealed class RollBackDebitedSparksOrchestrator : IRollBackDebitedSparksOrchestrator
    {
        private readonly ICurrencyDebitCompensatedService _currencyDebitCompensatedService;
        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly ILogger<RollBackDebitedSparksOrchestrator> _logger;
        private readonly LooksRatingDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly ISparksLedgerRepository _sparksLedgerRepository;
        private readonly ISparksDebitIdempotencyRepository _sparksDebitIdempotencyRepository;

        public RollBackDebitedSparksOrchestrator(
            ICurrencyDebitCompensatedService currencyDebitCompensatedService,
            IEventStoreRepository eventStoreRepository,
            ILogger<RollBackDebitedSparksOrchestrator> logger,
            LooksRatingDbContext context,
            IUserRepository userRepository,
            ISparksLedgerRepository sparksLedgerRepository,
            ISparksDebitIdempotencyRepository sparksDebitIdempotencyRepository)
        {
            _currencyDebitCompensatedService = currencyDebitCompensatedService;
            _eventStoreRepository = eventStoreRepository;
            _logger = logger;
            _context = context;
            _userRepository = userRepository;
            _sparksLedgerRepository = sparksLedgerRepository;
            _sparksDebitIdempotencyRepository = sparksDebitIdempotencyRepository;
        }

        public async Task<Result<RollBackDebitedSparksResponse>> RollBackDebitedSparks(
            long telegramId,
            int starsCount,
            string reason,
            string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByTelegramId(telegramId);
            if (user is null)
            {
                return Result.Success(new RollBackDebitedSparksResponse
                {
                    Success = false,
                    Message = "Пользователь не найден",
                });
            }

            if (!SparksGiftExchangeRules.TryGetSparksCost(starsCount, out var compensatedAmount))
            {
                return Result.Success(new RollBackDebitedSparksResponse
                {
                    Success = false,
                    Message = "Некорректная стоимость подарка",
                });
            }

            var sparks = await _sparksLedgerRepository.GetSparksByUserId(user.Id, cancellationToken);
            if (sparks is null)
            {
                return Result.Success(new RollBackDebitedSparksResponse
                {
                    Success = false,
                    Message = "Кошелёк искр не найден",
                });
            }

            var compensationReason = string.IsNullOrWhiteSpace(reason)
                ? "gift_delivery_failed"
                : reason.Trim();

            Guid debitEventId;
            decimal amountToCompensate;

            if (idempotencyKey is not null)
            {
                if (!IdempotencyKeyService.TryNormalizeClientKey(idempotencyKey, out var normalizedKey))
                {
                    return Result.Success(new RollBackDebitedSparksResponse
                    {
                        Success = false,
                        Message = "Ключ идемпотентности не указан",
                    });
                }

                var idempotencyRecord = await _sparksDebitIdempotencyRepository.GetByUserIdAndIdempotencyKey(
                    user.Id,
                    normalizedKey);
                if (idempotencyRecord is null)
                {
                    return Result.Success(new RollBackDebitedSparksResponse
                    {
                        Success = false,
                        Message = "Списание по ключу не найдено",
                    });
                }

                if (idempotencyRecord.CompensatedAt is not null)
                {
                    return Result.Success(new RollBackDebitedSparksResponse
                    {
                        Success = true,
                        Message = "Списание искр успешно отменено",
                    });
                }

                if (idempotencyRecord.StarsCount != starsCount)
                {
                    return Result.Success(new RollBackDebitedSparksResponse
                    {
                        Success = false,
                        Message = "Некорректная стоимость подарка",
                    });
                }

                debitEventId = idempotencyRecord.DebitEventId;
                amountToCompensate = idempotencyRecord.SparksAmount;
            }
            else
            {
                var lastEvent = await _eventStoreRepository.GetLastEvent(sparks.Id);
                if (lastEvent is not CurrencyDebitedEvent debitEvent)
                {
                    return Result.Success(new RollBackDebitedSparksResponse
                    {
                        Success = false,
                        Message = "Последнее событие списания не найдено",
                    });
                }

                debitEventId = debitEvent.EventId;
                amountToCompensate = compensatedAmount;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _currencyDebitCompensatedService.Compensate(
                    user.Id,
                    amountToCompensate,
                    debitEventId,
                    compensationReason,
                    cancellationToken);

                if (idempotencyKey is not null
                    && IdempotencyKeyService.TryNormalizeClientKey(idempotencyKey, out var normalizedKey))
                {
                    var idempotencyRecord = await _sparksDebitIdempotencyRepository.GetByUserIdAndIdempotencyKey(
                        user.Id,
                        normalizedKey);
                    idempotencyRecord?.MarkCompensated();
                }

                await _sparksLedgerRepository.SaveChangesAsync(cancellationToken);
                await _sparksDebitIdempotencyRepository.SaveChangesAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Rollback sparks debit failed for telegramId={TelegramId}", telegramId);
                return Result.Success(new RollBackDebitedSparksResponse
                {
                    Success = false,
                    Message = "Не удалось откатить списание искр",
                });
            }

            return Result.Success(new RollBackDebitedSparksResponse
            {
                Success = true,
                Message = "Списание искр успешно отменено",
            });
        }
    }
}
