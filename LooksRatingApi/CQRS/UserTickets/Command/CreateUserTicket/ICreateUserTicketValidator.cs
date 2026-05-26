using CSharpFunctionalExtensions;

namespace LooksRatingApi.Cqrs.UserTickets.Command.CreateUserTicket
{
    public interface ICreateUserTicketValidator
    {
        Task<Result<string>> ValidateAsync(CreateUserTicketCommand command, CancellationToken cancellationToken);
    }
}
