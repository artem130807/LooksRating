using CSharpFunctionalExtensions;

namespace LooksRatingApi.Contracts
{
    public interface IPhotoProfileModerationService
    {
        Task<Result> DismissTicketAsync(Guid ticketId, long adminTelegramId, CancellationToken cancellationToken = default);
        Task<Result> DeleteReportedProfileAsync(Guid ticketId, long adminTelegramId, CancellationToken cancellationToken = default);
    }
}
