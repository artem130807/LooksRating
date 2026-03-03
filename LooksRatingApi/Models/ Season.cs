using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public ICollection<PhotoSeason> PhotoSeasons {get; set;}
        public DateTime CreatedDate {get; set;}
        public Season()
        {
            PhotoSeasons = new List<PhotoSeason>();
        }
    }
}