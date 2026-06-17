namespace LooksRatingApi.CQRS.Payments.Command.CreatePaymentOrder
{
    public static class CreatePaymentOrderErrors
    {
        public const string UserNotFound = "UserNotFound";
        public const string ProductNotFound = "ProductNotFound";
        public const string InvalidTelegramId = "TelegramIdIsRequired";
        public const string InvalidProductCode = "ProductCodeIsRequired";
        public const string VipAlreadyActive = "VipAlreadyActive";
    }
}
