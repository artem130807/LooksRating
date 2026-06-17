namespace LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto
{
    public sealed class SetUserPhotoRequest
    {
        public long TelegramId { get; set; }
        public string TelegramFileId { get; set; } = string.Empty;
        public PhotoNominationRequest Nomination { get; set; } = new();
    }
}
