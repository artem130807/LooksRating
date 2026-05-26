using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.UserTicketContracts;

namespace LooksRatingApi.Cqrs.UserTickets.Command.CreateUserTicket
{
    public sealed class CreateUserTicketValidator : ICreateUserTicketValidator
    {
        private const int MaxDescriptionLength = 500;

        private readonly IUserRepository _userRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly IUserTicketRepository _userTicketRepository;

        public CreateUserTicketValidator(
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository,
            IUserTicketRepository userTicketRepository)
        {
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
            _userTicketRepository = userTicketRepository;
        }

        public async Task<Result<string>> ValidateAsync(CreateUserTicketCommand command, CancellationToken cancellationToken)
        {
            if (command.ReporterTelegramId <= 0)
            {
                return Result.Failure<string>(CreateUserTicketErrors.ReporterTelegramIdIsRequired);
            }

            if (command.PhotoUserId == Guid.Empty)
            {
                return Result.Failure<string>(CreateUserTicketErrors.PhotoUserIdIsRequired);
            }

            if (string.IsNullOrWhiteSpace(command.Description))
            {
                return Result.Failure<string>(CreateUserTicketErrors.DescriptionIsRequired);
            }

            if (command.Description.Trim().Length > MaxDescriptionLength)
            {
                return Result.Failure<string>(CreateUserTicketErrors.DescriptionTooLong);
            }

            var reporter = await _userRepository.GetUserByTelegramId(command.ReporterTelegramId);
            if (reporter is null)
            {
                return Result.Failure<string>(CreateUserTicketErrors.ReporterNotFound);
            }

            var photoUser = await _photoUserRepository.GePhotoUserById(command.PhotoUserId);
            if (photoUser is null)
            {
                return Result.Failure<string>(CreateUserTicketErrors.PhotoUserNotFound);
            }

            if (photoUser.UserId == reporter.Id)
            {
                return Result.Failure<string>(CreateUserTicketErrors.SelfComplaintIsNotAllowed);
            }

            var alreadyExists = await _userTicketRepository.ExistsByReporterAndPhoto(reporter.Id, photoUser.Id);
            if (alreadyExists)
            {
                return Result.Failure<string>(CreateUserTicketErrors.TicketAlreadyExists);
            }

            return Result.Success(string.Empty);
        }
    }
}
