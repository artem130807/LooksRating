using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.Cqrs.UserTickets.Command.CreateUserTicket
{
    public sealed record CreateUserTicketCommand(
        long ReporterTelegramId,
        Guid PhotoUserId,
        string Description) : IRequest<Result<CreateUserTicketResult>>;
}
