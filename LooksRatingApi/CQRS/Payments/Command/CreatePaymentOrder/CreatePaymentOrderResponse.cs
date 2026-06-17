namespace LooksRatingApi.CQRS.Payments.Command.CreatePaymentOrder
{
    public sealed class CreatePaymentOrderResponse
    {
        public Guid OrderId { get; set; }
        public string Payload { get; set; } = string.Empty;
        public int ProductCode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int AmountStars { get; set; }
        public string Currency { get; set; } = "XTR";
    }
}
