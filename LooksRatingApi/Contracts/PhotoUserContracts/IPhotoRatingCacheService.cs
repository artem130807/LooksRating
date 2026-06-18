using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IPhotoRatingCacheService
    {
        Task SyncPhotoRatingAsync(PhotoRatedEvent photoRated, CancellationToken cancellationToken = default);
        Task MarkProfileAsRatedAsync(Guid reviewerUserId, Guid seasonId, Guid photoProfileId, CancellationToken cancellationToken = default);
        Task ResetProfileRatingAsync(
            Guid profileId,
            Guid seasonId,
            string previousCity,
            string newCity,
            CancellationToken cancellationToken = default);

        Task ClearRatedMarkersForProfileAsync(
            Guid photoProfileId,
            Guid seasonId,
            IReadOnlyCollection<Guid> reviewerUserIds,
            CancellationToken cancellationToken = default);

        Task SyncProfileDisplayNameAsync(
            Guid profileId,
            string displayName,
            CancellationToken cancellationToken = default);
    }
}
