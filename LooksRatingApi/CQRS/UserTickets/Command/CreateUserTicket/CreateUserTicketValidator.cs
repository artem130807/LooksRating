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
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IUserTicketRepository _userTicketRepository;

        public CreateUserTicketValidator(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            IUserTicketRepository userTicketRepository)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _userTicketRepository = userTicketRepository;
        }

        public async Task<Result<string>> ValidateAsync(CreateUserTicketCommand command, CancellationToken cancellationToken)
        {
            if (command.ReporterTelegramId <= 0)
            {
                return Result.Failure<string>(CreateUserTicketErrors.ReporterTelegramIdIsRequired);
            }

            if (command.PhotoProfileId == Guid.Empty)
            {
                return Result.Failure<string>(CreateUserTicketErrors.PhotoProfileIdIsRequired);
            }
            var profileId = command.PhotoProfileId;

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

            var photoProfile = await _photoProfileRepository.GetByIdAsync(profileId, cancellationToken);
            if (photoProfile is null)
            {
                return Result.Failure<string>(CreateUserTicketErrors.PhotoProfileNotFound);
            }

            if (photoProfile.UserId == reporter.Id)
            {
                return Result.Failure<string>(CreateUserTicketErrors.SelfComplaintIsNotAllowed);
            }

            var alreadyExists = await _userTicketRepository.ExistsByReporterAndProfile(reporter.Id, photoProfile.Id);
            if (alreadyExists)
            {
                return Result.Failure<string>(CreateUserTicketErrors.TicketAlreadyExists);
            }

            return Result.Success(string.Empty);
        }
    }
}
