using LooksRatingApi.Enums;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestVipPhotos
{
    public sealed class GetTheBestVipPhotosRequest
    {
        public long TelegramId { get; set; }
        public GenderEnum GenderEnum { get; set; }
        public int Age { get; set; }
    }
}
