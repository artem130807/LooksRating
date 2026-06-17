using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace LooksRatingApi.Models
{
    public class ListSeasons
    {
        public Guid Id {get; set;}
        public ICollection<Season> Seasons {get; set;}
        public DateTime CreatedDate {get; set;}
        public ListSeasons()
        {
            Seasons = new List<Season>();
        }
        public static Result<ListSeasons> Create()
        {
            var list = new ListSeasons
            {
                Id = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow  
            };
            return list;
        }
    }
}