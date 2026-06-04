using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Models
{
    public sealed class PhotoProfile
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid SeasonId { get; set; }
        public Season Season { get; set; } = null!;
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public RankEnum Rank { get; set; }
        public StatusEnum Status { get; set; }
        public CityVo CityNomination { get; set; } = null!;
        public int AgeNomination { get; set; }
        public GenderEnum GenderNomination { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<PhotoProfilePhoto> Photos { get; set; } = new List<PhotoProfilePhoto>();
        public ICollection<UserTicket> UserTickets { get; set; } = new List<UserTicket>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<PhotoUser> LegacyPhotoUsers { get; set; } = new List<PhotoUser>();

        public void AddRating(decimal newRating)
        {
            var totalRating = Rating * RatingCount;
            RatingCount++;
            Rating = (totalRating + newRating) / RatingCount;
        }

        public void ChangeRating(decimal oldRating, decimal newRating)
        {
            if (RatingCount == 0)
            {
                return;
            }

            var totalRating = Rating * RatingCount - oldRating + newRating;
            Rating = totalRating / RatingCount;
        }

        public void UpdateRank(RankEnum rank) => Rank = rank;
        public void UpdateStatus(StatusEnum status) => Status = status;
    }
}
