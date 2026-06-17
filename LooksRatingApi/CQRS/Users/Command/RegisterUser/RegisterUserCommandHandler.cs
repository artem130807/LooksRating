using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.SparksLedgerContracts;
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
        private const decimal RegistrationBonusSparks = 10m;

        private readonly IUserRepository _userRepository;
        private readonly IUserRegisterValidator _validator;
        private readonly IUserSessionRepository _userSessionRepository;
        private readonly ISparksLedgerRepository _sparksLedgerRepository;
        private readonly LooksRatingDbContext _context;
        private readonly ILogger<RegisterUserCommandHandler> _logger;

        public RegisterUserCommandHandler(
            IUserRepository userRepository,
            IUserRegisterValidator validator,
            IUserSessionRepository userSessionRepository,
            ISparksLedgerRepository sparksLedgerRepository,
            LooksRatingDbContext context,
            ILogger<RegisterUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _validator = validator;
            _userSessionRepository = userSessionRepository;
            _sparksLedgerRepository = sparksLedgerRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<Result<RegisterUserResult>> Handle(
            RegisterUserCommand request,
            CancellationToken cancellationToken)
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

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _userRepository.Create(user);

                var sparksWalletResult = SparksWallet.Create(user.Id, RegistrationBonusSparks);
                if (sparksWalletResult.IsFailure)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<RegisterUserResult>(sparksWalletResult.Error);
                }

                await _sparksLedgerRepository.AddAsync(sparksWalletResult.Value, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                var sessionLinkResult = await EnsureSessionLinkedAsync(user, cancellationToken);
                if (sessionLinkResult.IsFailure)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<RegisterUserResult>(sessionLinkResult.Error);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(
                    ex,
                    "User registration failed for TelegramId {TelegramId}",
                    request.TelegramId);
                return Result.Failure<RegisterUserResult>(RegisterUserErrors.RegistrationFailed);
            }

            return Result.Success(new RegisterUserResult
            {
                UserId = user.Id,
                TelegramId = user.TelegramId,
                TelegramUsername = user.TelegramUsername,
                DisplayName = UserPublicDisplayName.Resolve(user),
            });
        }

        private async Task<Result> EnsureSessionLinkedAsync(User user, CancellationToken cancellationToken)
        {
            var session = await _userSessionRepository.GetByTelegramIdForUpdateAsync(
                user.TelegramId,
                cancellationToken);

            if (session is null)
            {
                var createResult = UserSession.Create(user.TelegramId, BotSessionState.Registered);
                if (createResult.IsFailure)
                {
                    return Result.Failure(createResult.Error);
                }

                var linkResult = createResult.Value.LinkUser(user.Id);
                if (linkResult.IsFailure)
                {
                    return linkResult;
                }

                await _userSessionRepository.CreateAsync(createResult.Value, cancellationToken);
                return Result.Success();
            }

            var linkExistingResult = session.LinkUser(user.Id);
            if (linkExistingResult.IsFailure)
            {
                return linkExistingResult;
            }

            await _userSessionRepository.UpdateAsync(session, cancellationToken);
            return Result.Success();
        }
    }
}
