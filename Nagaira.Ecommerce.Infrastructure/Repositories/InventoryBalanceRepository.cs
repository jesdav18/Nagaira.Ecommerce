using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class InventoryBalanceRepository : Repository<InventoryBalance>, IInventoryBalanceRepository
{
    public InventoryBalanceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<InventoryBalance?> GetByProductIdAsync(Guid productId)
    {
        return await _context.Set<InventoryBalance>()
            .Include(b => b.Product)
            .FirstOrDefaultAsync(b => b.ProductId == productId);
    }

    public async Task<IEnumerable<InventoryBalance>> GetLowStockProductsAsync(int threshold)
    {
        return await _context.Set<InventoryBalance>()
            .Include(b => b.Product)
            .Where(b => b.AvailableQuantity <= threshold && b.AvailableQuantity >= 0)
            .OrderBy(b => b.AvailableQuantity)
            .ToListAsync();
    }
}

