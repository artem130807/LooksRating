namespace LooksRatingApi.Cqrs.UserTickets.Command.CreateUserTicket
{
    public sealed class CreateUserTicketRequest
    {
        public long ReporterTelegramId { get; set; }
        public Guid PhotoUserId { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
