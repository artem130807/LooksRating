using System.Text.Json.Serialization;
using LooksRatingApi.Domain.Base;

namespace LooksRatingApi.Domain.DomainEvents
{
    public class PhotoRatedEvent : DomainEvent
    {
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public string City { get; set; } = string.Empty;
        public Guid SeasonId { get; set; }

        [JsonConstructor]
        private PhotoRatedEvent() { }

        public PhotoRatedEvent(
            Guid photoId,
            decimal rating,
            int ratingCount,
            string city,
            Guid seasonId)
        {
            AggregateId = photoId;
            Rating = rating;
            RatingCount = ratingCount;
            City = city;
            SeasonId = seasonId;
        }
    }
}
