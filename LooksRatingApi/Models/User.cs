using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public long TelegramId { get; set; }
        public string? TelegramUsername { get; set; }
        public int? Age { get; set; }
        public GenderEnum Gender { get; set; }
        public CityVo City { get; set; }
        public int TimesInTop { get; set; }

        public Guid PhotoUserId { get; set; }
        public PhotoUser PhotoUser { get; set; } = null!;
        public ICollection<UserTicket> UserTickets { get; set; } = new List<UserTicket>();
        public ICollection<TheBestWeek> TheBestWeeks { get; set; } = new List<TheBestWeek>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<PhotoSeason> PhotoSeasons { get; set; } = new List<PhotoSeason>();
    }
}