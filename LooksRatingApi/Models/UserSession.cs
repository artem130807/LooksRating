using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Models
{
    public class UserSession
    {
        public Guid Id { get; set; }
        public long TelegramId { get; set; }
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string State { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }

        public static Result<UserSession> Create(long telegramId, BotSessionState state = BotSessionState.Start)
        {
            if (telegramId <= 0)
                return Result.Failure<UserSession>("TelegramId обязателен");

            return new UserSession
            {
                Id = Guid.NewGuid(),
                TelegramId = telegramId,
                State = state.ToString(),
                UpdatedAt = DateTime.UtcNow
            };
        }

        public Result SetState(BotSessionState state)
        {
            State = state.ToString();
            UpdatedAt = DateTime.UtcNow;
            return Result.Success();
        }

        public Result LinkUser(Guid userId)
        {
            if (userId == Guid.Empty)
                return Result.Failure("UserId обязателен");

            UserId = userId;
            State = BotSessionState.Registered.ToString();
            UpdatedAt = DateTime.UtcNow;
            return Result.Success();
        }

        public void ResetForReregistration()
        {
            UserId = null;
            State = BotSessionState.Start.ToString();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
