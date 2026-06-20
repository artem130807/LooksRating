using LooksRatingApi.Models;
using LooksRatingApi.Services;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Infrastructure.Startup
{
    internal static class VipProductBootstrap
    {
        public static async Task EnsureConfiguredAsync(
            LooksRatingDbContext dbContext,
            CancellationToken cancellationToken = default)
        {
            var vipProduct = await dbContext.Products
                .FirstOrDefaultAsync(p => p.ProductCode == VipTopRules.VipProductCode, cancellationToken);

            if (vipProduct is null)
            {
                var productResult = Product.Create(
                    "VIP-статус",
                    VipTopRules.VipProductCode,
                    VipTopRules.VipStarsPrice,
                    "XTR",
                    VipTopRules.DefaultVipDays);
                if (productResult.IsSuccess)
                {
                    dbContext.Products.Add(productResult.Value);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                return;
            }

            if (vipProduct.CountStars == VipTopRules.VipStarsPrice
                && vipProduct.IsActive
                && string.Equals(vipProduct.Currency, "XTR", StringComparison.OrdinalIgnoreCase)
                && vipProduct.VipDays == VipTopRules.DefaultVipDays)
            {
                return;
            }

            dbContext.Entry(vipProduct).Property(nameof(Product.CountStars)).CurrentValue = VipTopRules.VipStarsPrice;
            dbContext.Entry(vipProduct).Property(nameof(Product.IsActive)).CurrentValue = true;
            dbContext.Entry(vipProduct).Property(nameof(Product.Currency)).CurrentValue = "XTR";
            dbContext.Entry(vipProduct).Property(nameof(Product.VipDays)).CurrentValue = VipTopRules.DefaultVipDays;
            dbContext.Entry(vipProduct).Property(nameof(Product.UpdatedAt)).CurrentValue = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
