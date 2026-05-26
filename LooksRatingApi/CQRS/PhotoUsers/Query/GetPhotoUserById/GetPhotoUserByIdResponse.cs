using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetPhotoUserById
{
    public class GetPhotoUserByIdResponse
    {
        public Guid Id {get; set;}
        public string Image {get; set;}
        public string UserName {get; set;}
        public int Age {get; set;}
        public string City {get; set;}
        public string Gender {get; set;}
        public string Rank {get; set; }
        public decimal Rating {get; set;}
        public decimal RatingCount {get; set;}
    }
}