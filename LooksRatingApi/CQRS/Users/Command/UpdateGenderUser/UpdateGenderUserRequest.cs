using LooksRatingApi.Enums;

namespace LooksRatingApi.CQRS.Users.Command.UpdateGenderUser
{
    public sealed class UpdateGenderUserRequest
    {
        public long TelegramId { get; set; }
        public GenderEnum Gender { get; set; }
    }
}
