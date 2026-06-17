namespace LooksRatingApi.CQRS.Users.Command.UpdateUserAge
{
    public sealed class UpdateUserAgeRequest
    {
        public long TelegramId { get; set; }
        public int Age { get; set; }
    }
}
