using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.UserTickets.Query.GetUserTicketById
{
    public sealed record GetUserTicketByIdQuery(Guid Id) : IRequest<Result<GetUserTicketByIdResponse>>;
}
