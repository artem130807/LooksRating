using System;
using System.Collections.Generic;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public long TelegramId { get; set; }
        public string? TelegramUsername { get; set; }
        public string? Name { get; set; }
        public int CountInTop {get; set;}
        public SparksWallet? SparksWallet { get; set; }
        public VipStatus Status {get; set;} = VipStatus.Unavaillable;
        public RecomendationSettings? RecomendationSettings { get; set; }
        public ICollection<PhotoUser> PhotoUsers { get; set; } = new List<PhotoUser>();
        public ICollection<PhotoProfile> PhotoProfiles { get; set; } = new List<PhotoProfile>();
        public ICollection<UserTicket> UserTickets { get; set; } = new List<UserTicket>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public void UpdateVipStatus() => Status = VipStatus.Availlable;
    }
}
