using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetUserPhotos
{
    public class GetUserPhotosItem
    {
        public Guid Id { get; set; }
        public string TelegramFileId { get; set; } = string.Empty;
    }

    public class GetUserPhotosResponse
    {
        public Guid ProfileId { get; set; }
        public Guid UserId { get; set; }
        public long RecipientTelegramId { get; set; }
        public string Rank {get; set;} = string.Empty;
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string City {get; set;} = string.Empty;
        public string? DisplayName { get; set; }
        public int TimesInTop { get; set; }
        public int? SeasonTopPlace { get; set; }
        public int? SeasonTopTotal { get; set; }
        public List<GetUserPhotosItem> Photos { get; set; } = new();
    }
}