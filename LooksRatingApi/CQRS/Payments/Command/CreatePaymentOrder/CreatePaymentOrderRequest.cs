namespace LooksRatingApi.CQRS.Payments.Command.CreatePaymentOrder
{
    public sealed class CreatePaymentOrderRequest
    {
        public long TelegramId { get; set; }
        public int ProductCode { get; set; }
    }
}
