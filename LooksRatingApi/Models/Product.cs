using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace LooksRatingApi.Models
{
    public class Product
    {
        public Guid Id {get; private set;}
        public string Name {get; private set;}
        public int ProductCode {get; private set;}
        public int CountStars {get; private set;}
        public string Currency {get; private set;}
        public int VipDays {get; private set;}
        public bool IsActive {get; private set;}
        public DateTime CreatedAt {get; private set;}
        public DateTime UpdatedAt {get; private set;}
        
        public static Result<Product> Create(
            string name,
            int productCode,
            int countStars,
            string currency = "XTR",
            int vipDays = 30)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                ProductCode = productCode,
                CountStars = countStars,
                Currency = currency,
                VipDays = vipDays,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            return product;
        }

        public void UpdateIsActive(bool isActive)
        {
            IsActive = isActive;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}