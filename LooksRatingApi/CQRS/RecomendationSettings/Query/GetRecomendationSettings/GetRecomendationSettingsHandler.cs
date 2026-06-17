using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.RecomendationSettingsContracts;
using LooksRatingApi.Contracts.UserContracts;
using MediatR;

namespace LooksRatingApi.CQRS.RecomendationSettings.Query.GetRecomendationSettings
{
    public sealed class GetRecomendationSettingsHandler
        : IRequestHandler<GetRecomendationSettingsQuery, Result<GetRecomendationSettingsResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRecomendationSettingsRepository _recomendationSettingsRepository;

        public GetRecomendationSettingsHandler(
            IUserRepository userRepository,
            IRecomendationSettingsRepository recomendationSettingsRepository)
        {
            _userRepository = userRepository;
            _recomendationSettingsRepository = recomendationSettingsRepository;
        }

        public async Task<Result<GetRecomendationSettingsResponse>> Handle(
            GetRecomendationSettingsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.TelegramId <= 0)
            {
                return Result.Failure<GetRecomendationSettingsResponse>(RecomendationSettingsErrors.TelegramIdIsRequired);
            }

            var user = await _userRepository.GetUserByTelegramId(request.TelegramId);
            if (user is null)
            {
                return Result.Failure<GetRecomendationSettingsResponse>(RecomendationSettingsErrors.UserNotFound);
            }

            var settings = user.RecomendationSettings
                ?? await _recomendationSettingsRepository.GetByUserIdAsync(user.Id, cancellationToken);

            if (settings is null || !settings.IsComplete)
            {
                return Result.Success(new GetRecomendationSettingsResponse
                {
                    IsConfigured = false
                });
            }

            return Result.Success(new GetRecomendationSettingsResponse
            {
                Age = settings.Age,
                Gender = settings.Gender,
                City = settings.City.Value ?? string.Empty,
                IsConfigured = true
            });
        }
    }
}
