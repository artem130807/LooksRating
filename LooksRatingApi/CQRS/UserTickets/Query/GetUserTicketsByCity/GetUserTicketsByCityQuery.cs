using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.UserTickets.Query.GetUserTicketsByCity
{
    public sealed record GetUserTicketsByCityQuery(string City) : IRequest<Result<List<GetUserTicketsByCityResponse>>>;
}
