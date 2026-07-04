namespace LooksRatingApi.Models
{
    public enum OutboxMessageStatus
    {
        Pending = 0,
        Processing = 1,
        Failed = 2,
        Completed = 3
    }
}
