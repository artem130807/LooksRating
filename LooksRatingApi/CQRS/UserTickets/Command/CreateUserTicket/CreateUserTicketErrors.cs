namespace LooksRatingApi.Cqrs.UserTickets.Command.CreateUserTicket
{
    public static class CreateUserTicketErrors
    {
        public const string ReporterTelegramIdIsRequired = "ReporterTelegramIdIsRequired";
        public const string PhotoUserIdIsRequired = "PhotoUserIdIsRequired";
        public const string DescriptionIsRequired = "DescriptionIsRequired";
        public const string DescriptionTooLong = "DescriptionTooLong";
        public const string ReporterNotFound = "ReporterNotFound";
        public const string PhotoUserNotFound = "PhotoUserNotFound";
        public const string SelfComplaintIsNotAllowed = "SelfComplaintIsNotAllowed";
        public const string TicketAlreadyExists = "TicketAlreadyExists";
    }
}
