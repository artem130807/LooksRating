using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IPhotoRatingCacheService
    {
        Task SyncPhotoRatingAsync(PhotoRatedEvent photoRated, CancellationToken cancellationToken = default);
        Task MarkProfileAsRatedAsync(Guid reviewerUserId, Guid seasonId, Guid photoProfileId, CancellationToken cancellationToken = default);
    }
}
