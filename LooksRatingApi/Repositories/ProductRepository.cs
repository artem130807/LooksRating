using LooksRatingApi.Contracts.ProductContracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public sealed class ProductRepository : IProductRepository
    {
        private readonly LooksRatingDbContext _context;

        public ProductRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public Task<Product?> GetByCodeAsync(int productCode, CancellationToken cancellationToken = default)
        {
            return _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductCode == productCode && p.IsActive, cancellationToken);
        }
    }
}
