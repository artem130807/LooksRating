using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto
{
    public sealed class RecreateAllUserPhotosRequest
    {
        public long TelegramId { get; set; }
        public List<string> TelegramFileIds { get; set; } = new();
        public PhotoNominationRequest Nomination { get; set; } = new();
    }
}
