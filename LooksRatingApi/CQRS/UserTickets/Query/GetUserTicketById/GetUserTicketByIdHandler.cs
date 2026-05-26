using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.UserTickets.Query.GetUserTicketById
{
    public sealed class GetUserTicketByIdHandler
        : IRequestHandler<GetUserTicketByIdQuery, Result<GetUserTicketByIdResponse>>
    {
        private readonly IUserTicketRepository _userTicketRepository;

        public GetUserTicketByIdHandler(IUserTicketRepository userTicketRepository)
        {
            _userTicketRepository = userTicketRepository;
        }

        public async Task<Result<GetUserTicketByIdResponse>> Handle(
            GetUserTicketByIdQuery request,
            CancellationToken cancellationToken)
        {
            var ticket = await _userTicketRepository.GetTicketById(request.Id);
            if (ticket is null)
            {
                return Result.Failure<GetUserTicketByIdResponse>("Жалоба не найдена");
            }

            return Result.Success(new GetUserTicketByIdResponse
            {
                Id = ticket.Id,
                Description = ticket.Description,
                OccuredAt = ticket.OccuredAt,
                ReporterUserId = ticket.UserId,
                ReporterTelegramId = ticket.User.TelegramId,
                ReporterDisplayName = UserPublicDisplayName.Resolve(ticket.User),
                ReporterCity = ticket.User.RecomendationSettings?.City.Value ?? string.Empty,
                PhotoUserId = ticket.PhotoUserId,
                PhotoTelegramFileId = ticket.PhotoUser.TelegramFileId,
                PhotoOwnerUserId = ticket.PhotoUser.UserId
            });
        }
    }
}
