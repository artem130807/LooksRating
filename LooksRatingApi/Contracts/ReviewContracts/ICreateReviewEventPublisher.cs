using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Contracts.ReviewContracts
{
    public interface ICreateReviewEventPublisher
    {
        Task<CreateReviewEvent> PublishAsync(
            Guid reviewerId,
            Guid photoProfileId,
            CancellationToken cancellationToken);
    }
}
