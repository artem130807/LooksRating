namespace LooksRatingApi.CQRS.UserTickets.Query.GetUserTicketsByCity
{
    public sealed class GetUserTicketsByCityRequest
    {
        public string City { get; set; } = string.Empty;
    }
}
