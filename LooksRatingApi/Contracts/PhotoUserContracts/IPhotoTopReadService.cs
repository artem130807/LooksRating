using LooksRatingApi.Enums;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IPhotoTopReadService
    {
        Task<(IReadOnlyList<Guid> PhotoIds, int TotalCount)> GetTopPhotoIdsAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string normalizedCity,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
    }
}
