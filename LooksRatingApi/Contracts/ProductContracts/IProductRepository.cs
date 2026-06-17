using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.ProductContracts
{
    public interface IProductRepository
    {
        Task<Product?> GetByCodeAsync(int productCode, CancellationToken cancellationToken = default);
    }
}
