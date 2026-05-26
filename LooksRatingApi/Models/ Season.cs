using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace LooksRatingApi.Models
{
    public class  Season
    {
        public Guid Id {get; set;}
        public string Name {get; set;}
        public int Number {get; set;}
        public bool IsClosed {get; set;}
        public Guid ListSeasonsId {get; set;}
        public ListSeasons ListSeasons {get; set;}
        public ICollection<PhotoUser> PhotoUsers {get; set;}
        public DateTime CreatedDate {get; set;}

        public static Result<Season> Create(string name, int number, Guid listSeasonsId)
        {
            var season = new Season
            {
                Id = Guid.NewGuid(),
                Name = name, 
                Number =  number,
                IsClosed = false,
                ListSeasonsId = listSeasonsId,
                CreatedDate = DateTime.UtcNow
            };
            return season;
        }
    }
}