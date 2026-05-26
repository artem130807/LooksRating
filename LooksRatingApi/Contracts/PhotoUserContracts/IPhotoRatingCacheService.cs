using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IPhotoRatingCacheService
    {
        Task SyncPhotoRatingAsync(PhotoRatedEvent photoRated, CancellationToken cancellationToken = default);
        Task MarkPhotoAsRatedAsync(Guid reviewerUserId, Guid photoUserId, CancellationToken cancellationToken = default);
    }
}
