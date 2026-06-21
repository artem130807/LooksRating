using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Enums;
using LooksRatingGrpc;
using DomainOutputStatusEnum = LooksRatingApi.Enums.OutputStatusEnum;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Services.Orchestrators;

public sealed class UpdateStatusWritingOffSparksOrchestrator : IUpdateStatusWritingOffSparksOrchestrator
{
    private readonly IWritingOffSparksRepository _writingOffSparksRepository;
    private readonly ICurrencyDebitCompensatedService _currencyDebitCompensatedService;
    private readonly ISparksLedgerRepository _sparksLedgerRepository;
    private readonly ISparksDebitIdempotencyRepository _sparksDebitIdempotencyRepository;
    private readonly LooksRatingDbContext _context;
    private readonly ILogger<UpdateStatusWritingOffSparksOrchestrator> _logger;

    public UpdateStatusWritingOffSparksOrchestrator(
        IWritingOffSparksRepository writingOffSparksRepository,
        ICurrencyDebitCompensatedService currencyDebitCompensatedService,
        ISparksLedgerRepository sparksLedgerRepository,
        ISparksDebitIdempotencyRepository sparksDebitIdempotencyRepository,
        LooksRatingDbContext context,
        ILogger<UpdateStatusWritingOffSparksOrchestrator> logger)
    {
        _writingOffSparksRepository = writingOffSparksRepository;
        _currencyDebitCompensatedService = currencyDebitCompensatedService;
        _sparksLedgerRepository = sparksLedgerRepository;
        _sparksDebitIdempotencyRepository = sparksDebitIdempotencyRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<UpdateStatusWritingOffSparksResponse>> UpdateStatusAsync(
        Guid writingOffSparksId,
        DomainOutputStatusEnum status,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(status))
        {
            return Result.Success(new UpdateStatusWritingOffSparksResponse
            {
                Success = false,
                Message = "Некорректный статус списания искр",
            });
        }

        if (status is not (DomainOutputStatusEnum.Confirmed or DomainOutputStatusEnum.Cancelled))
        {
            return Result.Success(new UpdateStatusWritingOffSparksResponse
            {
                Success = false,
                Message = "Допустимы только статусы «Выполнена» и «Отменена»",
            });
        }

        var writingOffSparks = await _writingOffSparksRepository.GetById(writingOffSparksId);
        if (writingOffSparks is null)
        {
            return Result.Success(new UpdateStatusWritingOffSparksResponse
            {
                Success = false,
                Message = "Списание искр не найдено",
            });
        }

        if (writingOffSparks.Status != DomainOutputStatusEnum.Pending)
        {
            return Result.Success(new UpdateStatusWritingOffSparksResponse
            {
                Success = false,
                Message = "Статус заявки уже изменён",
            });
        }

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            if (status == DomainOutputStatusEnum.Cancelled)
            {
                await _currencyDebitCompensatedService.Compensate(
                    writingOffSparks.UserId,
                    writingOffSparks.SparksCount,
                    writingOffSparks.Id,
                    "writing_off_cancelled",
                    cancellationToken);
                await _sparksLedgerRepository.SaveChangesAsync(cancellationToken);

                var idempotencyRecord = await _sparksDebitIdempotencyRepository.GetByUserIdAndIdempotencyKey(
                    writingOffSparks.UserId,
                    writingOffSparks.IdempotencyKey);
                idempotencyRecord?.MarkCompensated();
                await _sparksDebitIdempotencyRepository.SaveChangesAsync(cancellationToken);
            }

            writingOffSparks.UpdateStatus(status);
            await _writingOffSparksRepository.SaveChanges();

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update WritingOffSparks status. Id={WritingOffSparksId}, Status={Status}",
                writingOffSparksId,
                status);

            return Result.Success(new UpdateStatusWritingOffSparksResponse
            {
                Success = false,
                Message = "Не удалось обновить статус списания искр",
            });
        }

        return Result.Success(new UpdateStatusWritingOffSparksResponse
        {
            Success = true,
            Message = "Статус списания искр обновлён",
        });
    }
}
