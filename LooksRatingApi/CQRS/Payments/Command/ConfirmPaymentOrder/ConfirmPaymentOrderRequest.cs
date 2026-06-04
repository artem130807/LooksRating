namespace LooksRatingApi.CQRS.Payments.Command.ConfirmPaymentOrder
{
    public sealed class ConfirmPaymentOrderRequest
    {
        public long TelegramId { get; set; }
        public string Payload { get; set; } = string.Empty;
        public string TelegramPaymentChargeId { get; set; } = string.Empty;
        public string? ProviderPaymentChargeId { get; set; }
    }
}
