namespace LooksRatingApi.CQRS.UserTickets.Query.GetUserTicketsByCity
{
    public sealed class GetUserTicketsByCityResponse
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime OccuredAt { get; set; }
        public Guid ReporterUserId { get; set; }
        public long ReporterTelegramId { get; set; }
        public Guid PhotoUserId { get; set; }
        public string PhotoTelegramFileId { get; set; } = string.Empty;
    }
}
