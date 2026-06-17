namespace LooksRatingApi.CQRS.Payments.Command.ConfirmPaymentOrder
{
    public static class ConfirmPaymentOrderErrors
    {
        public const string InvalidTelegramId = "TelegramIdIsRequired";
        public const string PayloadIsRequired = "PayloadIsRequired";
        public const string TelegramChargeIdIsRequired = "TelegramPaymentChargeIdIsRequired";
        public const string OrderNotFound = "PaymentOrderNotFound";
        public const string OrderOwnerMismatch = "PaymentOrderOwnerMismatch";
        public const string PaymentAlreadyBound = "PaymentAlreadyBoundToAnotherOrder";
    }
}
