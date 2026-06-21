using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Models;
using LooksRatingGrpc;
using LooksRatingApi.Services;
using LooksRatingApi.Services.SparksWallet;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LooksRatingApi.Services.Orchestrators;

public sealed class CreateWritingOffSparksOrchestrator : ICreateWritingOffSparksOrchestrator
{
    private readonly IWritingOffSparksRepository _writingOffSparksRepository;
    private readonly IUserRepository _userRepository;
    private readonly LooksRatingDbContext _context;
    private readonly ILogger<CreateWritingOffSparksOrchestrator> _logger;
    private readonly IPhotoProfileRepository _photoProfileRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly ISparksDebitIdempotencyRepository _sparksDebitIdempotencyRepository;

    public CreateWritingOffSparksOrchestrator(
        IWritingOffSparksRepository writingOffSparksRepository,
        IUserRepository userRepository,
        LooksRatingDbContext context,
        ILogger<CreateWritingOffSparksOrchestrator> logger,
        IPhotoProfileRepository photoProfileRepository,
        ISeasonRepository seasonRepository,
        ISparksDebitIdempotencyRepository sparksDebitIdempotencyRepository)
    {
        _writingOffSparksRepository = writingOffSparksRepository;
        _userRepository = userRepository;
        _context = context;
        _logger = logger;
        _photoProfileRepository = photoProfileRepository;
        _seasonRepository = seasonRepository;
        _sparksDebitIdempotencyRepository = sparksDebitIdempotencyRepository;
    }

    public async Task<Result<CreateWritingOffSparksResponse>> ConfirmedWriting(
        long telegramId,
        decimal sparksCount,
        string key,
        int starsCount,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserByTelegramId(telegramId);
        if (user is null)
        {
            return Result.Success(new CreateWritingOffSparksResponse
            {
                Success = false,
                Message = "Пользователь не найден",
            });
        }

        if (!IdempotencyKeyService.TryNormalizeClientKey(key, out var normalizedKey))
        {
            return Result.Success(new CreateWritingOffSparksResponse
            {
                Success = false,
                Message = "Ключ идемпотентности не указан",
            });
        }

        var existing = await _writingOffSparksRepository.GetByUserIdAndIdempotencyKey(user.Id, normalizedKey);
        if (existing is not null)
        {
            if (existing.Status == Enums.OutputStatusEnum.Cancelled)
            {
                if (existing.SparksCount != sparksCount || existing.Stars != starsCount)
                {
                    return Result.Success(new CreateWritingOffSparksResponse
                    {
                        Success = false,
                        Message = "Недопустимая стоимость обмена",
                    });
                }

                var debitFailure = await GetActiveDebitValidationFailureAsync(
                    user.Id,
                    normalizedKey,
                    sparksCount,
                    starsCount);
                if (debitFailure is not null)
                {
                    return Result.Success(debitFailure);
                }

                var reactivate = existing.Reactivate(sparksCount, starsCount);
                if (reactivate.IsFailure)
                {
                    return Result.Success(new CreateWritingOffSparksResponse
                    {
                        Success = false,
                        Message = "Не удалось зафиксировать списание искр",
                    });
                }

                await _writingOffSparksRepository.SaveChanges();
                return Result.Success(new CreateWritingOffSparksResponse
                {
                    Success = true,
                    Message = "Успешно",
                });
            }

            return Result.Success(new CreateWritingOffSparksResponse
            {
                Success = true,
                Message = "Заявка уже создана",
            });
        }

        var currentSeason = await _seasonRepository.GetCurrent();
        if (currentSeason is null)
        {
            return Result.Success(new CreateWritingOffSparksResponse
            {
                Success = false,
                Message = "Сезон не найден",
            });
        }

        var photoProfile = await _photoProfileRepository.GetByUserAndSeasonAsync(
            user.Id,
            currentSeason.Id,
            cancellationToken);
        if (photoProfile is null)
        {
            return Result.Success(new CreateWritingOffSparksResponse
            {
                Success = false,
                Message = "Добавьте фотографию в сезон",
            });
        }

        if (!SparksGiftExchangeRules.TryGetSparksCost(starsCount, out var expectedSparks)
            || expectedSparks != sparksCount)
        {
            return Result.Success(new CreateWritingOffSparksResponse
            {
                Success = false,
                Message = "Недопустимая стоимость обмена",
            });
        }

        var newDebitFailure = await GetActiveDebitValidationFailureAsync(
            user.Id,
            normalizedKey,
            sparksCount,
            starsCount);
        if (newDebitFailure is not null)
        {
            return Result.Success(newDebitFailure);
        }

        var writingOffSparks = WritingOffSparks.Create(
            user.Id,
            sparksCount,
            normalizedKey,
            starsCount,
            photoProfile.CityNomination.Value);
        if (writingOffSparks.IsFailure)
        {
            return Result.Success(new CreateWritingOffSparksResponse
            {
                Success = false,
                Message = writingOffSparks.Error,
            });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _writingOffSparksRepository.Add(writingOffSparks.Value);
            await _writingOffSparksRepository.SaveChanges();
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateIdempotencyKey(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            var duplicate = await _writingOffSparksRepository.GetByUserIdAndIdempotencyKey(user.Id, normalizedKey);
            if (duplicate is not null)
            {
                return Result.Success(new CreateWritingOffSparksResponse
                {
                    Success = true,
                    Message = "Заявка уже создана",
                });
            }

            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Failed to create WritingOffSparks for telegramId={TelegramId}",
                telegramId);

            return Result.Success(new CreateWritingOffSparksResponse
            {
                Success = false,
                Message = "Не удалось зафиксировать списание искр",
            });
        }

        return Result.Success(new CreateWritingOffSparksResponse
        {
            Success = true,
            Message = "Успешно",
        });
    }

    private async Task<CreateWritingOffSparksResponse?> GetActiveDebitValidationFailureAsync(
        Guid userId,
        string normalizedKey,
        decimal sparksCount,
        int starsCount)
    {
        var debit = await _sparksDebitIdempotencyRepository.GetByUserIdAndIdempotencyKey(
            userId,
            normalizedKey);
        if (debit is null || debit.CompensatedAt is not null)
        {
            return new CreateWritingOffSparksResponse
            {
                Success = false,
                Message = "Списание искр не найдено",
            };
        }

        if (debit.SparksAmount != sparksCount || debit.StarsCount != starsCount)
        {
            return new CreateWritingOffSparksResponse
            {
                Success = false,
                Message = "Недопустимая стоимость обмена",
            };
        }

        return null;
    }

    private static bool IsDuplicateIdempotencyKey(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
