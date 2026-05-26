using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Enums;

namespace LooksRatingApi.CQRS.Users.Command.UpdateGenderUser
{
    public sealed class UpdateGenderUserValidator : IUpdateGenderUserValidator
    {
        private readonly IUserRepository _userRepository;

        public UpdateGenderUserValidator(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<string>> ValidateAsync(
            UpdateGenderUserCommandCommand command,
            CancellationToken cancellationToken)
        {
            if (command.telegramId <= 0)
            {
                return Result.Failure<string>(UpdateGenderUserErrors.TelegramIdIsRequired);
            }

            if (!Enum.IsDefined(typeof(GenderEnum), command.gender) || command.gender == GenderEnum.Unknown)
            {
                return Result.Failure<string>(UpdateGenderUserErrors.InvalidGender);
            }

            var user = await _userRepository.GetUserByTelegramId(command.telegramId);
            if (user is null)
            {
                return Result.Failure<string>(UpdateGenderUserErrors.UserNotFound);
            }

            return Result.Success(string.Empty);
        }
    }
}
