namespace LooksRatingApi.Cqrs.UserTickets.Command.CreateUserTicket
{
    public sealed class CreateUserTicketResult
    {
        public Guid TicketId { get; set; }
        public Guid ReporterUserId { get; set; }
        public Guid PhotoProfileId { get; set; }
        public DateTime OccuredAt { get; set; }
    }
}
