using LooksRatingApi.Enums;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IPhotoTopReadService
    {
        Task<(IReadOnlyList<Guid> ProfileIds, int TotalCount)> GetTopProfileIdsAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string cityNomination,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default);
    }
}
