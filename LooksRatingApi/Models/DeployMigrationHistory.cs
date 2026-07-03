namespace LooksRatingApi.Models
{
    public sealed class DeployMigrationHistory
    {
        public string Name { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }
}
