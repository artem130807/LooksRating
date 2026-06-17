using LooksRatingApi.Enums;

namespace LooksRatingApi.CQRS.UserSessions.Command.UpdateUserSessionState
{
    public sealed class UpdateUserSessionStateRequest
    {
        public long TelegramId { get; set; }
        public BotSessionState State { get; set; }
    }
}
