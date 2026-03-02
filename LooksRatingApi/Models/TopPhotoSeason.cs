using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Models
{
    public class TopPhotoSeason
    {
        public Guid Id {get ; set;}
        public CityVo City { get; set; }
        public GenderEnum GenderEnum {get; set;}
        public Guid PhotoSeasonId { get; set; }
        public PhotoSeason PhotoSeason { get; set; }
        public int Place { get; set; }
    }
}