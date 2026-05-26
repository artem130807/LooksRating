using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.UserSessionContracts;
using LooksRatingApi.CQRS.UserSessions;
using MediatR;

namespace LooksRatingApi.CQRS.UserSessions.Command.LinkUserSession
{
    public sealed class LinkUserSessionHandler
        : IRequestHandler<LinkUserSessionCommand, Result<UserSessionResponse>>
    {
        private readonly IUserSessionRepository _userSessionRepository;
        private readonly IUserRepository _userRepository;

        public LinkUserSessionHandler(
            IUserSessionRepository userSessionRepository,
            IUserRepository userRepository)
        {
            _userSessionRepository = userSessionRepository;
            _userRepository = userRepository;
        }

        public async Task<Result<UserSessionResponse>> Handle(
            LinkUserSessionCommand command,
            CancellationToken cancellationToken)
        {
            if (command.TelegramId <= 0)
                return Result.Failure<UserSessionResponse>("TelegramId обязателен");

            if (command.UserId == Guid.Empty)
                return Result.Failure<UserSessionResponse>("UserId обязателен");

            var user = await _userRepository.GetUserByTelegramId(command.TelegramId);
            if (user is null || user.Id != command.UserId)
                return Result.Failure<UserSessionResponse>("Пользователь не найден");

            var session = await _userSessionRepository.GetByTelegramIdForUpdateAsync(
                command.TelegramId,
                cancellationToken);

            if (session is null)
                return Result.Failure<UserSessionResponse>("Сессия не найдена");

            var linkResult = session.LinkUser(command.UserId);
            if (linkResult.IsFailure)
                return Result.Failure<UserSessionResponse>(linkResult.Error);

            await _userSessionRepository.UpdateAsync(session, cancellationToken);

            session.User = user;
            return Result.Success(UserSessionMapping.ToResponse(session));
        }
    }
}
