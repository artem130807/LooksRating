using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Models
{
    public class UserSession
    {
        public Guid Id { get; set; }
        public long TelegramId { get; set; }
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string State { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}