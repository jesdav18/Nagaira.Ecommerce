using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IProductPriceRepository : IRepository<ProductPrice>
{
    Task<ProductPrice?> GetByProductAndLevelAsync(Guid productId, Guid priceLevelId);
    Task<IEnumerable<ProductPrice>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<ProductPrice>> GetByPriceLevelIdAsync(Guid priceLevelId);
    Task<decimal?> GetPriceForProductAndLevelAsync(Guid productId, Guid? priceLevelId);
}

