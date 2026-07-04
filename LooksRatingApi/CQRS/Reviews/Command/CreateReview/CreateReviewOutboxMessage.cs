namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public static class CreateReviewOutboxMessage
    {
        public const string Type = "CreateReviewSideEffects.v1";
    }

    public sealed class CreateReviewOutboxPayload
    {
        public Guid ReviewId { get; init; }
        public Guid ReviewerUserId { get; init; }
        public long ReviewerTelegramId { get; init; }
        public Guid PhotoProfileId { get; init; }
        public Guid SeasonId { get; init; }
        public bool IsNewReview { get; init; }
        public decimal UpdatedProfileRating { get; init; }
        public int UpdatedProfileRatingCount { get; init; }
        public string ProfileCity { get; init; } = string.Empty;
        public Guid ProfileOwnerUserId { get; init; }
        public long? ProfileOwnerTelegramId { get; init; }
    }

    public sealed record CreateReviewOutboxState
    {
        public bool CacheSynced { get; init; }
        public bool PhotoRatedEventPublished { get; init; }
        public bool CreateReviewEventPublished { get; init; }
        public bool ReviewerRewardGranted { get; init; }
        public bool ProfileRewardGranted { get; init; }
        public bool LastActiveUpdated { get; init; }

        public static CreateReviewOutboxState Initial(bool isNewReview) =>
            new()
            {
                CreateReviewEventPublished = !isNewReview
            };
    }
}
