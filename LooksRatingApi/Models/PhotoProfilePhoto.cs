namespace LooksRatingApi.Models
{
    public sealed class PhotoProfilePhoto
    {
        public Guid Id { get; set; }
        public Guid PhotoProfileId { get; set; }
        public PhotoProfile PhotoProfile { get; set; } = null!;
        public string TelegramFileId { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
