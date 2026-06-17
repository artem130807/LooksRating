using CSharpFunctionalExtensions;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IUnviewablePhotosProfilesService
    {
        Task<Result> AddUnviewablePhotosProfile(
            Guid photoProfileId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<Result> RemoveUnviewablePhotosProfile(
            Guid photoProfileId,
            IReadOnlyCollection<Guid> reporterUserIds,
            CancellationToken cancellationToken = default);

        Task<Result<List<Guid>>> GetUnviewablePhotosProfile(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
