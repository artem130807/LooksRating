using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Models
{
    public class TheBestWeek
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public ICollection<PhotoUser> PhotoUsers { get; set; } = new List<PhotoUser>();
    }
}