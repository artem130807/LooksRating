using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Services.PhotoServices;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LooksRatingApi.Cqrs.UserTickets.Command.CreateUserTicket
{
    public sealed class CreateUserTicketCommandHandler
        : IRequestHandler<CreateUserTicketCommand, Result<CreateUserTicketResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserTicketRepository _userTicketRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ICreateUserTicketValidator _validator;
        private readonly IUnviewablePhotosProfilesService _unviewablePhotosProfilesService;
        private readonly ILogger<CreateUserTicketCommandHandler> _logger;

        public CreateUserTicketCommandHandler(
            IUserRepository userRepository,
            IUserTicketRepository userTicketRepository,
            IPhotoProfileRepository photoProfileRepository,
            ICreateUserTicketValidator validator,
            IUnviewablePhotosProfilesService unviewablePhotosProfilesService,
            ILogger<CreateUserTicketCommandHandler> logger)
        {
            _userRepository = userRepository;
            _userTicketRepository = userTicketRepository;
            _photoProfileRepository = photoProfileRepository;
            _validator = validator;
            _unviewablePhotosProfilesService = unviewablePhotosProfilesService;
            _logger = logger;
        }

        public async Task<Result<CreateUserTicketResult>> Handle(
            CreateUserTicketCommand request,
            CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<CreateUserTicketResult>(validationResult.Error);
            }

            var reporter = await _userRepository.GetUserByTelegramId(request.ReporterTelegramId);
            if (reporter is null)
            {
                return Result.Failure<CreateUserTicketResult>(CreateUserTicketErrors.ReporterNotFound);
            }

            var profile = await _photoProfileRepository.GetByIdAsync(request.PhotoProfileId, cancellationToken);
            if (profile is null)
            {
                return Result.Failure<CreateUserTicketResult>(CreateUserTicketErrors.PhotoProfileNotFound);
            }
            if (await _userTicketRepository.IsPhotoProfileLockedAsync(request.PhotoProfileId, cancellationToken))
                return Result.Failure<CreateUserTicketResult>("Профиль временно недоступен для жалоб");
            var occuredAt = DateTime.UtcNow;
            var ticket = new UserTicket
            {
                Id = Guid.NewGuid(),
                Description = request.Description.Trim(),
                OccuredAt = occuredAt,
                UserId = reporter.Id,
                PhotoProfileId = request.PhotoProfileId
            };
            await _userTicketRepository.Create(ticket);
            var cacheResult = await _unviewablePhotosProfilesService.AddUnviewablePhotosProfile(
                ticket.PhotoProfileId,
                ticket.UserId,
                cancellationToken);
            if (cacheResult.IsFailure)
            {
                _logger.LogWarning(
                    "Ticket {TicketId} created but unviewable profile cache update failed for user {UserId}, profile {PhotoProfileId}: {Error}",
                    ticket.Id,
                    ticket.UserId,
                    ticket.PhotoProfileId,
                    cacheResult.Error);
            }
            return Result.Success(new CreateUserTicketResult
            {
                TicketId = ticket.Id,
                ReporterUserId = reporter.Id,
                PhotoProfileId = request.PhotoProfileId,
                OccuredAt = occuredAt
            });
        }
    }
}
