using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Filters
{
    public class PhotoFilter
    {
        public int Age {get; set;}
        public GenderEnum GenderEnum {get; set;}
        public string City {get; set;}
    }
}