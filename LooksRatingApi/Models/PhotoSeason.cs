using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Models
{
    public class PhotoSeason
    {
        public Guid Id {get; set;}
        public string TelegramFileId { get; set; } = string.Empty;
        public string Rank {get; set;}
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public Guid SeasonId {get; set;}
        public Guid UserId {get; set;}
        public DateTime SnapshotAt { get; set; }
        public Season Season {get; set;}
        public User User {get; set;}
        public ICollection<TopPhotoSeason> TopPhotoSeasons {get; set;}
        public PhotoSeason()
        {
            TopPhotoSeasons = new List<TopPhotoSeason>();
        }
    }
}