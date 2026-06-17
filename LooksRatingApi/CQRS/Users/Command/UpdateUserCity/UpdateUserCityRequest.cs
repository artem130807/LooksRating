namespace LooksRatingApi.CQRS.Users.Command.UpdateUserCity
{
    public sealed class UpdateUserCityRequest
    {
        public long TelegramId { get; set; }
        public string City { get; set; } = string.Empty;
    }
}
