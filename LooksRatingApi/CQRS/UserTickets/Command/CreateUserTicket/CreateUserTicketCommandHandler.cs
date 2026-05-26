using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingApi.Models;
using MediatR;

namespace LooksRatingApi.Cqrs.UserTickets.Command.CreateUserTicket
{
    public sealed class CreateUserTicketCommandHandler
        : IRequestHandler<CreateUserTicketCommand, Result<CreateUserTicketResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserTicketRepository _userTicketRepository;
        private readonly ICreateUserTicketValidator _validator;

        public CreateUserTicketCommandHandler(
            IUserRepository userRepository,
            IUserTicketRepository userTicketRepository,
            ICreateUserTicketValidator validator)
        {
            _userRepository = userRepository;
            _userTicketRepository = userTicketRepository;
            _validator = validator;
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

            var occuredAt = DateTime.UtcNow;
            var ticket = new UserTicket
            {
                Id = Guid.NewGuid(),
                Description = request.Description.Trim(),
                OccuredAt = occuredAt,
                UserId = reporter.Id,
                PhotoUserId = request.PhotoUserId
            };

            await _userTicketRepository.Create(ticket);

            return Result.Success(new CreateUserTicketResult
            {
                TicketId = ticket.Id,
                ReporterUserId = reporter.Id,
                PhotoUserId = request.PhotoUserId,
                OccuredAt = occuredAt
            });
        }
    }
}
