using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Contracts.ReviewContracts
{
    public interface IReviewMilestoneNotifier
    {
        Task TryNotifyAsync(CreateReviewEvent reviewEvent, CancellationToken cancellationToken);
    }
}
