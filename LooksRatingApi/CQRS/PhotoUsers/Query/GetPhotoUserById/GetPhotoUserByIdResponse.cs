using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetPhotoUserById
{
    public class GetPhotoUserByIdImageResponse
    {
        public Guid Id { get; set; }
        public string TelegramFileId { get; set; } = string.Empty;
    }

    public class GetPhotoUserByIdResponse
    {
        public Guid Id {get; set;}
        public Guid ProfileId { get; set; }
        public string Image {get; set;}
        public IReadOnlyList<GetPhotoUserByIdImageResponse> Images { get; set; } = Array.Empty<GetPhotoUserByIdImageResponse>();
        public string UserName {get; set;}
        public int Age {get; set;}
        public string City {get; set;}
        public string Gender {get; set;}
        public string Rank {get; set; }
        public decimal Rating {get; set;}
        public decimal RatingCount {get; set;}
    }
}