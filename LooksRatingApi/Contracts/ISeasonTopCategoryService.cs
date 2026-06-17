namespace LooksRatingApi.Contracts
{
    public interface ISeasonTopCategoryService
    {
        Task<IReadOnlyList<VipTopCategory>> GetQualifiedCategoriesForSeasonAsync(
            Guid seasonId,
            bool seasonIsClosed,
            CancellationToken cancellationToken = default);
    }
}
