using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserSessionContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.CQRS.UserSessions;
using LooksRatingApi.Models;
using MediatR;

namespace LooksRatingApi.CQRS.UserSessions.Command.EnsureUserSession
{
    public sealed class EnsureUserSessionHandler
        : IRequestHandler<EnsureUserSessionCommand, Result<UserSessionResponse>>
    {
        private readonly IUserSessionRepository _userSessionRepository;

        public EnsureUserSessionHandler(IUserSessionRepository userSessionRepository)
        {
            _userSessionRepository = userSessionRepository;
        }

        public async Task<Result<UserSessionResponse>> Handle(
            EnsureUserSessionCommand command,
            CancellationToken cancellationToken)
        {
            if (command.TelegramId <= 0)
                return Result.Failure<UserSessionResponse>("TelegramId обязателен");

            var existing = await _userSessionRepository.GetByTelegramIdAsync(command.TelegramId, cancellationToken);
            if (existing is not null)
                return Result.Success(UserSessionMapping.ToResponse(existing));

            var initialState = command.InitialState ?? BotSessionState.Start;
            var createResult = UserSession.Create(command.TelegramId, initialState);
            if (createResult.IsFailure)
                return Result.Failure<UserSessionResponse>(createResult.Error);

            await _userSessionRepository.CreateAsync(createResult.Value, cancellationToken);
            return Result.Success(UserSessionMapping.ToResponse(createResult.Value));
        }
    }
}
