using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IInventoryBalanceRepository : IRepository<InventoryBalance>
{
    Task<InventoryBalance?> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<InventoryBalance>> GetLowStockProductsAsync(int threshold);
}

