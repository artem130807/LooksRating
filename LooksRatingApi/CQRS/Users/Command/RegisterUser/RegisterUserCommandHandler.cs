using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.UserSessionContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.Cqrs.Users.Command.RegisterUser
{
    public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserRegisterValidator _validator;
        private readonly IUserSessionRepository _userSessionRepository;

        public RegisterUserCommandHandler(
            IUserRepository userRepository,
            IUserRegisterValidator validator,
            IUserSessionRepository userSessionRepository)
        {
            _userRepository = userRepository;
            _validator = validator;
            _userSessionRepository = userSessionRepository;
        }

        public async Task<Result<RegisterUserResult>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<RegisterUserResult>(validationResult.Error);
            }

            var displayNameResult = UserDisplayNameFactory.Create(
                request.UseTelegramUsernameAsDisplay,
                request.TelegramUsername,
                request.Name);
            if (displayNameResult.IsFailure)
            {
                return Result.Failure<RegisterUserResult>(displayNameResult.Error);
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                TelegramId = request.TelegramId,
                TelegramUsername = string.IsNullOrWhiteSpace(request.TelegramUsername)
                    ? null
                    : request.TelegramUsername.Trim().TrimStart('@'),
                Name = displayNameResult.Value,
            };

            await _userRepository.Create(user);

            await EnsureSessionLinkedAsync(user, cancellationToken);

            return Result.Success(new RegisterUserResult
            {
                UserId = user.Id,
                TelegramId = user.TelegramId,
                TelegramUsername = user.TelegramUsername,
                DisplayName = UserPublicDisplayName.Resolve(user),
            });
        }

        private async Task EnsureSessionLinkedAsync(User user, CancellationToken cancellationToken)
        {
            var session = await _userSessionRepository.GetByTelegramIdForUpdateAsync(
                user.TelegramId,
                cancellationToken);

            if (session is null)
            {
                var createResult = UserSession.Create(user.TelegramId, BotSessionState.Registered);
                if (createResult.IsFailure)
                    return;

                createResult.Value.LinkUser(user.Id);
                await _userSessionRepository.CreateAsync(createResult.Value, cancellationToken);
                return;
            }

            session.LinkUser(user.Id);
            await _userSessionRepository.UpdateAsync(session, cancellationToken);
        }
    }
}
