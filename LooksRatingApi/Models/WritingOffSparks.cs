using System;
using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;
using LooksRatingApi.Services;

namespace LooksRatingApi.Models
{
    public class WritingOffSparks
    {
        public Guid Id {get; private set;}
        public OutputStatusEnum Status {get; private set;}
        public Guid UserId {get; private set;}
        public string IdempotencyKey { get; private set; } = null!;
        public User User {get; private set;}
        public string City {get; private set;}
        public decimal SparksCount { get; private set; }
        public int Stars {get; private set;}
        public DateTime CreatedAt {get; private set;}
        
        private WritingOffSparks(){}
        public static Result<WritingOffSparks> Create(Guid userId, decimal sparksCount, string idempotencyKey, int stars, string city)
        {
            if (!IdempotencyKeyService.TryNormalizeClientKey(idempotencyKey, out var normalizedKey))
            {
                return Result.Failure<WritingOffSparks>("SparksLedgerIdempotencyKeyIsTooLong");
            }

            var writingOffSparks = new WritingOffSparks
            {
                Id = Guid.NewGuid(),
                Status = OutputStatusEnum.Pending,
                IdempotencyKey = normalizedKey,
                UserId = userId,
                SparksCount = sparksCount,
                City = city,
                Stars = stars,
                CreatedAt = DateTime.UtcNow
            };
            return writingOffSparks;
        }
        public void UpdateStatus(OutputStatusEnum status) => Status = status;

        public Result Reactivate(decimal sparksCount, int starsCount)
        {
            if (Status is not OutputStatusEnum.Cancelled)
            {
                return Result.Failure("WritingOffSparksNotCancelled");
            }

            if (SparksCount != sparksCount || Stars != starsCount)
            {
                return Result.Failure("WritingOffSparksAmountMismatch");
            }

            Status = OutputStatusEnum.Pending;
            return Result.Success();
        }
    }
}