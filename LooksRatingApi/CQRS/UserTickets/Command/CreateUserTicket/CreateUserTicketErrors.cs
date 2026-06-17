namespace LooksRatingApi.Cqrs.UserTickets.Command.CreateUserTicket
{
    public static class CreateUserTicketErrors
    {
        public const string ReporterTelegramIdIsRequired = "ReporterTelegramIdIsRequired";
        public const string PhotoProfileIdIsRequired = "PhotoProfileIdIsRequired";
        public const string DescriptionIsRequired = "DescriptionIsRequired";
        public const string DescriptionTooLong = "DescriptionTooLong";
        public const string ReporterNotFound = "ReporterNotFound";
        public const string PhotoProfileNotFound = "PhotoProfileNotFound";
        public const string SelfComplaintIsNotAllowed = "SelfComplaintIsNotAllowed";
        public const string TicketAlreadyExists = "TicketAlreadyExists";
    }
}
