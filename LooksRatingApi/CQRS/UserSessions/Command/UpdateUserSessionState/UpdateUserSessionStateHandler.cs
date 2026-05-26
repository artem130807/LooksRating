using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserSessionContracts;
using LooksRatingApi.CQRS.UserSessions;
using MediatR;

namespace LooksRatingApi.CQRS.UserSessions.Command.UpdateUserSessionState
{
    public sealed class UpdateUserSessionStateHandler
        : IRequestHandler<UpdateUserSessionStateCommand, Result<UserSessionResponse>>
    {
        private readonly IUserSessionRepository _userSessionRepository;

        public UpdateUserSessionStateHandler(IUserSessionRepository userSessionRepository)
        {
            _userSessionRepository = userSessionRepository;
        }

        public async Task<Result<UserSessionResponse>> Handle(
            UpdateUserSessionStateCommand command,
            CancellationToken cancellationToken)
        {
            if (command.TelegramId <= 0)
                return Result.Failure<UserSessionResponse>("TelegramId обязателен");

            if (!Enum.IsDefined(command.State))
                return Result.Failure<UserSessionResponse>("Недопустимое состояние сессии");

            var session = await _userSessionRepository.GetByTelegramIdForUpdateAsync(
                command.TelegramId,
                cancellationToken);

            if (session is null)
                return Result.Failure<UserSessionResponse>("Сессия не найдена");

            var setResult = session.SetState(command.State);
            if (setResult.IsFailure)
                return Result.Failure<UserSessionResponse>(setResult.Error);

            await _userSessionRepository.UpdateAsync(session, cancellationToken);

            return Result.Success(UserSessionMapping.ToResponse(session));
        }
    }
}
