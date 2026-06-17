namespace LooksRatingApi.CQRS.Payments.Command.ConfirmPaymentOrder
{
    public sealed class ConfirmPaymentOrderResponse
    {
        public Guid OrderId { get; set; }
        public bool Paid { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
