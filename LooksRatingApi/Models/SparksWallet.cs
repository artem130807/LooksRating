using System.Security.Cryptography;
using CSharpFunctionalExtensions;
using LooksRatingApi.Domain.Base;
using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Models
{
    public sealed class SparksWallet : AggregateRoot
    {
        public const int IdempotencyKeyMaxLength = 128;
        private const string IdempotencyKeyPrefix = "sparks-wallet:";
        public Guid UserId { get; private set; }
        public User User { get; private set; } = null!;
        public decimal SparksCount { get; private set; }
        public string IdempotencyKey { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }

        public static Result<SparksWallet> Create(
            Guid userId,
            decimal sparksCount = 0m,
            string? idempotencyKey = null)
        {
            if (userId == Guid.Empty)
            {
                return Result.Failure<SparksWallet>("SparksLedgerUserIdIsRequired");
            }

            if (sparksCount < 0)
            {
                return Result.Failure<SparksWallet>("SparksLedgerAmountMustNotBeNegative");
            }

            var normalizedKey = NormalizeIdempotencyKey(idempotencyKey) ?? GenerateIdempotencyKey();
            if (normalizedKey.Length > IdempotencyKeyMaxLength)
            {
                return Result.Failure<SparksWallet>("SparksLedgerIdempotencyKeyIsTooLong");
            }

            var sparks = new SparksWallet();
            sparks.ApplyChange(new CreateSparksLedgerEvent(Guid.NewGuid(), userId, normalizedKey, sparksCount));
            return sparks;
        }

        public void AddSparksCount(decimal sparks) => SparksCount += sparks;
        public void WritingOffSparks(decimal sparks) => SparksCount -= sparks;

        private static string GenerateIdempotencyKey()
        {
            Span<byte> bytes = stackalloc byte[16];
            RandomNumberGenerator.Fill(bytes);
            return $"{IdempotencyKeyPrefix}{Convert.ToHexString(bytes).ToLowerInvariant()}";
        }

        private static string? NormalizeIdempotencyKey(string? idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return null;
            }

            return idempotencyKey.Trim();
        }

        private void Apply(CreateSparksLedgerEvent @event)
        {
            Id = @event.AggregateId;
            UserId = @event.UserId;
            SparksCount = @event.SparksCount;
            IdempotencyKey = @event.IdempotencyKey;
            CreatedAt = @event.CreatedAt;
        }

        private void Apply(CurrencySparksEvent @event)
        {
            SparksCount = @event.SparksCount;
        }

        private void Apply(CurrencyDebitedEvent @event)
        {
            SparksCount = @event.SparksCount;
        }

        private void Apply(CurrencyDebitCompensatedEvent @event)
        {
            SparksCount = @event.NewSparksCount;
        }
    }
}
