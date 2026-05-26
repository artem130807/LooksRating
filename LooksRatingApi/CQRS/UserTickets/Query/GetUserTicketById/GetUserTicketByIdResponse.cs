namespace LooksRatingApi.CQRS.UserTickets.Query.GetUserTicketById
{
    public sealed class GetUserTicketByIdResponse
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime OccuredAt { get; set; }
        public Guid ReporterUserId { get; set; }
        public long ReporterTelegramId { get; set; }
        public string? ReporterDisplayName { get; set; }
        public string ReporterCity { get; set; } = string.Empty;
        public Guid PhotoUserId { get; set; }
        public string PhotoTelegramFileId { get; set; } = string.Empty;
        public Guid PhotoOwnerUserId { get; set; }
    }
}
