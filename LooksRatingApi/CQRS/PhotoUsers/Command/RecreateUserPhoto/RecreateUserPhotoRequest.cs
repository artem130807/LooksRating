using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto
{
    public sealed class RecreateUserPhotoRequest
    {
        public long TelegramId { get; set; }
        public string TelegramFileId { get; set; } = string.Empty;
        public PhotoNominationRequest Nomination { get; set; } = new();
    }
}
