using System.Text.Json.Serialization;
using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Domain.DomainEvents
{
    public sealed class CreateReviewEvent : DomainEvent
    {
        public Guid ReviewerId { get; set; }
        public Guid PhotoProfileId { get; set; }
        public int ReviewsCount { get; set; }
        public bool IsNewReview { get; set; }

        [JsonConstructor]
        private CreateReviewEvent()
        {
        }

        public CreateReviewEvent(
            Guid reviewerId,
            Guid photoProfileId,
            int reviewsCount = 0,
            bool isNewReview = true)
        {
            AggregateId = reviewerId;
            ReviewerId = reviewerId;
            PhotoProfileId = photoProfileId;
            ReviewsCount = reviewsCount;
            IsNewReview = isNewReview;
        }
    }
}