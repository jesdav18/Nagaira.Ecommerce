using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class ProductPriceRepository : Repository<ProductPrice>, IProductPriceRepository
{
    public ProductPriceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ProductPrice?> GetByProductAndLevelAsync(Guid productId, Guid priceLevelId)
    {
        return await _dbSet
            .Include(p => p.PriceLevel)
            .FirstOrDefaultAsync(p => p.ProductId == productId 
                && p.PriceLevelId == priceLevelId 
                && p.IsActive 
                && !p.IsDeleted);
    }

    public async Task<IEnumerable<ProductPrice>> GetByProductIdAsync(Guid productId)
    {
        return await _dbSet
            .Include(p => p.PriceLevel)
            .Where(p => p.ProductId == productId && !p.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductPrice>> GetByPriceLevelIdAsync(Guid priceLevelId)
    {
        return await _dbSet
            .Include(p => p.Product)
            .Where(p => p.PriceLevelId == priceLevelId && !p.IsDeleted)
            .ToListAsync();
    }

    public async Task<decimal?> GetPriceForProductAndLevelAsync(Guid productId, Guid? priceLevelId)
    {
        if (!priceLevelId.HasValue) return null;

        var productPrice = await GetByProductAndLevelAsync(productId, priceLevelId.Value);
        if (productPrice == null) return null;
        
        return productPrice.Price;
    }
}

