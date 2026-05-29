using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetUserPhotos
{
    public class GetUserPhotosResponse
    {
        public Guid Id { get; set; }
        public string Rank {get; set;}
        public string TelegramFileId { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public Guid UserId { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string City {get; set;}
        public string? DisplayName { get; set; }
        public int TimesInTop { get; set; }
    }
}