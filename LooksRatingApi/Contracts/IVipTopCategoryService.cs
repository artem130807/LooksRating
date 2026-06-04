namespace LooksRatingApi.Contracts
{
    public interface IVipTopCategoryService
    {
        Task<IReadOnlyList<VipTopCategory>> GetQualifiedCategoriesAsync(CancellationToken cancellationToken = default);
    }
}
