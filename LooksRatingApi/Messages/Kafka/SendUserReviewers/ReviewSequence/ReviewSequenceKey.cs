using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence
{
    public readonly record struct ReviewSequenceKey(Guid PhotoProfileId)
    {
        public static ReviewSequenceKey From(CreateReviewEvent @event) => new(@event.PhotoProfileId);

        public static bool TryParseKafkaKey(string? key, out ReviewSequenceKey sequenceKey)
        {
            sequenceKey = default;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (Guid.TryParse(key, out var photoProfileId))
            {
                sequenceKey = new ReviewSequenceKey(photoProfileId);
                return true;
            }

            var parts = key.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && Guid.TryParse(parts[1], out photoProfileId))
            {
                sequenceKey = new ReviewSequenceKey(photoProfileId);
                return true;
            }

            return false;
        }

        public string ToKafkaKey() => PhotoProfileId.ToString("N");
    }
}
