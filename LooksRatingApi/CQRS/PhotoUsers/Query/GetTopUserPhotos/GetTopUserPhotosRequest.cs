using LooksRatingApi.Enums;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos
{
    public sealed class GetTopUserPhotosRequest
    {
        public long TelegramId { get; set; }
        public GenderEnum GenderEnum { get; set; }
        public int Age { get; set; }
        public Guid? SeasonId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
