using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Models
{
    public class UserTicket
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime OccuredAt { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}