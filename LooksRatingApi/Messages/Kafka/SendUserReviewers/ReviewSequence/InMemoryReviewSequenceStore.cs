using System.Collections.Concurrent;

namespace LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence
{
    public sealed class InMemoryReviewSequenceStore : IReviewSequenceStore
    {
        private readonly ConcurrentDictionary<ReviewSequenceKey, int> _counts = new();

        public int? GetLastReviewsCount(ReviewSequenceKey key)
        {
            return _counts.TryGetValue(key, out var count) ? count : null;
        }

        public void SetLastReviewsCount(ReviewSequenceKey key, int reviewsCount)
        {
            _counts[key] = reviewsCount;
        }

        public int ResolveNextReviewsCount(ReviewSequenceKey key, Func<int?, int> calculateNext)
        {
            var previous = GetLastReviewsCount(key);
            var next = calculateNext(previous);
            SetLastReviewsCount(key, next);
            return next;
        }
    }
}
