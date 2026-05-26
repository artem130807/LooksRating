using System;
using System.Collections.Generic;

namespace LooksRatingApi.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public long TelegramId { get; set; }
        public string? TelegramUsername { get; set; }
        public string? Name { get; set; }
        public RecomendationSettings? RecomendationSettings { get; set; }
        public ICollection<PhotoUser> PhotoUsers { get; set; } = new List<PhotoUser>();
        public ICollection<UserTicket> UserTickets { get; set; } = new List<UserTicket>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
