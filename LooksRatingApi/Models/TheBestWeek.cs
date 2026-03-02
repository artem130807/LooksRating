using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.DtoModels.ValueObjectDto;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Models
{
    public class TheBestWeek
    {
        public Guid Id { get; set; }
        public CityVo City {get; set;}
        public GenderEnum GenderEnumed {get; set;}
        public DateTime CreatedDate { get; set; }
        public ICollection<PhotoUser> PhotoUsers { get; set; } = new List<PhotoUser>();
    }
}