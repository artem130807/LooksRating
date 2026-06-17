namespace LooksRatingApi.Contracts.AdminModeration
{
    public sealed class ModerationCitiesResponse
    {
        public List<string> Cities { get; set; } = [];
    }

    public sealed class ModerationTicketPhotoDto
    {
        public string PhotoId { get; set; } = string.Empty;
        public string TelegramFileId { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public sealed class ModerationTicketSummaryDto
    {
        public string TicketId { get; set; } = string.Empty;
        public string PhotoProfileId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long OccuredAtUnix { get; set; }
        public long ReporterTelegramId { get; set; }
        public string ReporterDisplayName { get; set; } = string.Empty;
        public string ProfileCity { get; set; } = string.Empty;
        public long ProfileTelegramId { get; set; }
        public string ProfileDisplayName { get; set; } = string.Empty;
        public int ProfileAge { get; set; }
        public string ProfileGender { get; set; } = string.Empty;
        public double ProfileRating { get; set; }
        public int ProfileRatingCount { get; set; }
        public string ProfileRank { get; set; } = string.Empty;
    }

    public sealed class ModerationTicketsByCityResponse
    {
        public List<ModerationTicketSummaryDto> Tickets { get; set; } = [];
        public int TotalCount { get; set; }
    }

    public sealed class ModerationTicketDetailDto
    {
        public string TicketId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long OccuredAtUnix { get; set; }
        public long ReporterTelegramId { get; set; }
        public string ReporterDisplayName { get; set; } = string.Empty;
        public string ReporterCity { get; set; } = string.Empty;
        public string PhotoProfileId { get; set; } = string.Empty;
        public long ProfileTelegramId { get; set; }
        public string ProfileDisplayName { get; set; } = string.Empty;
        public string ProfileCity { get; set; } = string.Empty;
        public int ProfileAge { get; set; }
        public string ProfileGender { get; set; } = string.Empty;
        public double ProfileRating { get; set; }
        public int ProfileRatingCount { get; set; }
        public string ProfileRank { get; set; } = string.Empty;
        public List<ModerationTicketPhotoDto> Photos { get; set; } = [];
    }

    public sealed class ModerationQueuedTicketResponse
    {
        public string ResolvedCity { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int Offset { get; set; }
        public ModerationTicketDetailDto? Ticket { get; set; }
    }

    public sealed class ModerationTicketCountResponse
    {
        public string ResolvedCity { get; set; } = string.Empty;
        public int TotalCount { get; set; }
    }

    public sealed class ModerationActionRequest
    {
        public long AdminTelegramId { get; set; }
    }
}
