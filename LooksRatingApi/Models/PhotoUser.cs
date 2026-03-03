using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Models
{
    public class PhotoUser
    {
        public Guid Id { get; set; }
        public string TelegramFileId { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
    }
}