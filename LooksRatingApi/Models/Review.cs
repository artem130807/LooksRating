using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Models
{
    public class Review
    {
        public Guid Id { get; set; }
        public int Rating { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid PhotoUserId { get; set; }
        public PhotoUser PhotoUser { get; set; } = null!;
    }
}