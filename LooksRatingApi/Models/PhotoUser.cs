using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Models
{
    public class PhotoUser
    {
        public Guid Id { get; set; }
        public string TelegramFileId { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public RankEnum Rank {get; set;}
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid SeasonId {get; set;}
        public StatusEnum Status {get; set;}
        public Season Season {get; set;}
        public Guid? PhotoProfileId { get; set; }
        public PhotoProfile? PhotoProfile { get; set; }
        public CityVo CityNomination {get; set;}
        public int AgeNomination {get; set;}
        public GenderEnum GenderNomination {get; set;}
        public ICollection<UserTicket> UserTickets { get; set; } = new List<UserTicket>();
        public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
        
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