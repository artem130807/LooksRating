using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Services;

namespace LooksRatingApi.CQRS.Users.Command.UpdateUserAge
{
    public sealed class UpdateUserAgeValidator : IUpdateUserAgeValidator
    {
        private readonly IUserRepository _userRepository;

        public UpdateUserAgeValidator(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<string>> ValidateAsync(
            UpdateUserAgeCommand command,
            CancellationToken cancellationToken)
        {
            if (command.TelegramId <= 0)
            {
                return Result.Failure<string>(UpdateUserAgeErrors.TelegramIdIsRequired);
            }

            if (!TopService.IsValidFeedAge(command.Age))
            {
                return Result.Failure<string>(UpdateUserAgeErrors.InvalidAge);
            }

            var user = await _userRepository.GetUserByTelegramId(command.TelegramId);
            if (user is null)
            {
                return Result.Failure<string>(UpdateUserAgeErrors.UserNotFound);
            }

            return Result.Success(string.Empty);
        }
    }
}
