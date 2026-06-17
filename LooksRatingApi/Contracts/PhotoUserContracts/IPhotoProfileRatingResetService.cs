using LooksRatingApi.Models;
using LooksRatingApi.Services.PhotoProfiles;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IPhotoProfileRatingResetService
    {
        Task<IReadOnlyList<Guid>> ResetDatabaseAsync(
            PhotoProfile profile,
            CancellationToken cancellationToken = default);

        Task ResetCacheAsync(
            PhotoProfile profile,
            PhotoProfileNomination previousNomination,
            IReadOnlyCollection<Guid> reviewerUserIds,
            CancellationToken cancellationToken = default);
    }
}
