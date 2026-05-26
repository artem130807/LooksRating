namespace LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto
{
    public sealed class SetUserPhotoResult
    {
        public Guid PhotoUserId { get; init; }
        public Guid UserId { get; init; }
        public long TelegramId { get; init; }
        public string TelegramFileId { get; init; } = string.Empty;
        public decimal Rating { get; init; }
        public int RatingCount { get; init; }
        public string City {get; init;}
    }
}
