using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserSessionContracts;
using LooksRatingApi.CQRS.UserSessions;
using MediatR;

namespace LooksRatingApi.CQRS.UserSessions.Query.GetUserSession
{
    public sealed class GetUserSessionHandler : IRequestHandler<GetUserSessionQuery, Result<UserSessionResponse>>
    {
        private readonly IUserSessionRepository _userSessionRepository;

        public GetUserSessionHandler(IUserSessionRepository userSessionRepository)
        {
            _userSessionRepository = userSessionRepository;
        }

        public async Task<Result<UserSessionResponse>> Handle(
            GetUserSessionQuery query,
            CancellationToken cancellationToken)
        {
            if (query.TelegramId <= 0)
                return Result.Failure<UserSessionResponse>("TelegramId обязателен");

            var session = await _userSessionRepository.GetByTelegramIdAsync(query.TelegramId, cancellationToken);
            if (session is null)
                return Result.Failure<UserSessionResponse>("Сессия не найдена");

            return Result.Success(UserSessionMapping.ToResponse(session));
        }
    }
}
