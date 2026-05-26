using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.CQRS.UserSessions
{
    internal static class UserSessionMapping
    {
        public static UserSessionResponse ToResponse(UserSession session) =>
            new()
            {
                Id = session.Id,
                TelegramId = session.TelegramId,
                UserId = session.UserId,
                State = session.State,
                ParsedState = ParseState(session.State),
                IsRegistered = session.UserId.HasValue,
                UpdatedAt = session.UpdatedAt,
                TelegramUsername = session.User?.TelegramUsername,
                City = session.User?.RecomendationSettings?.City.Value
            };

        public static bool TryParseState(string state, out BotSessionState parsed) =>
            Enum.TryParse(state, ignoreCase: true, out parsed);

        private static BotSessionState ParseState(string state) =>
            TryParseState(state, out var parsed) ? parsed : BotSessionState.Start;
    }

    public sealed class UserSessionResponse
    {
        public Guid Id { get; set; }
        public long TelegramId { get; set; }
        public Guid? UserId { get; set; }
        public string State { get; set; } = string.Empty;
        public BotSessionState ParsedState { get; set; }
        public bool IsRegistered { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? TelegramUsername { get; set; }
        public string? City { get; set; }
    }
}
