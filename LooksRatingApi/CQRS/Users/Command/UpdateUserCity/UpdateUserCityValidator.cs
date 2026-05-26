using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.UserContracts;

namespace LooksRatingApi.CQRS.Users.Command.UpdateUserCity
{
    public sealed class UpdateUserCityValidator : IUpdateUserCityValidator
    {
        private readonly IUserRepository _userRepository;
        private readonly ICityService _cityService;
        private readonly INormalizeCityNameService _normalizeCityNameService;

        public UpdateUserCityValidator(
            IUserRepository userRepository,
            ICityService cityService,
            INormalizeCityNameService normalizeCityNameService)
        {
            _userRepository = userRepository;
            _cityService = cityService;
            _normalizeCityNameService = normalizeCityNameService;
        }

        public async Task<Result<string>> ValidateAsync(
            UpdateUserCityCommand command,
            CancellationToken cancellationToken)
        {
            if (command.TelegramId <= 0)
            {
                return Result.Failure<string>(UpdateUserCityErrors.TelegramIdIsRequired);
            }

            if (string.IsNullOrWhiteSpace(command.City))
            {
                return Result.Failure<string>(UpdateUserCityErrors.InvalidCity);
            }

            var normalizedCity = _normalizeCityNameService.Normalize(command.City);
            if (!_cityService.IsCityValid(normalizedCity))
            {
                return Result.Failure<string>(UpdateUserCityErrors.InvalidCity);
            }

            var user = await _userRepository.GetUserByTelegramId(command.TelegramId);
            if (user is null)
            {
                return Result.Failure<string>(UpdateUserCityErrors.UserNotFound);
            }

            return Result.Success(string.Empty);
        }
    }
}
