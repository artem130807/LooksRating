using CSharpFunctionalExtensions;

using LooksRatingApi.Contracts.UserContracts;

using LooksRatingApi.Enums;



namespace LooksRatingApi.Cqrs.Users.Command.RegisterUser

{

    public sealed class UserRegisterValidator : IUserRegisterValidator

    {

        private readonly IUserRepository _userRepository;



        public UserRegisterValidator(IUserRepository userRepository)

        {

            _userRepository = userRepository;

        }



        public async Task<Result<string>> ValidateAsync(RegisterUserCommand command, CancellationToken cancellationToken)

        {

            if (command.TelegramId <= 0)

            {

                return Result.Failure<string>(RegisterUserErrors.TelegramIdIsRequired);

            }



            if (!string.IsNullOrWhiteSpace(command.TelegramUsername) && command.TelegramUsername.Length > 32)

            {

                return Result.Failure<string>(RegisterUserErrors.InvalidTelegramUsername);

            }



            var existingUser = await _userRepository.GetUserByTelegramId(command.TelegramId);

            if (existingUser is not null)

            {

                return Result.Failure<string>(RegisterUserErrors.UserAlreadyExists);

            }



            var displayNameResult = UserDisplayNameFactory.Create(

                command.UseTelegramUsernameAsDisplay,

                command.TelegramUsername,

                command.Name);

            if (displayNameResult.IsFailure)

            {

                return Result.Failure<string>(displayNameResult.Error);

            }



            return Result.Success(string.Empty);

        }

    }

}

