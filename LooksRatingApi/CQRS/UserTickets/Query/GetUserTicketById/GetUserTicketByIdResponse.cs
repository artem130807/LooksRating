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
        public Guid PhotoProfileId { get; set; }
        public IReadOnlyList<string> PhotoTelegramFileIds { get; set; } = Array.Empty<string>();
        public Guid PhotoOwnerUserId { get; set; }
    }
}
