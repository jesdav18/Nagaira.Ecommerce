using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class InventoryMovementRepository : Repository<InventoryMovement>, IInventoryMovementRepository
{
    public InventoryMovementRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<InventoryMovement>> GetByProductIdAsync(Guid productId)
    {
        return await _dbSet
            .Include(m => m.Product)
            .Include(m => m.Creator)
            .Where(m => m.ProductId == productId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryMovement>> GetByReferenceAsync(string referenceType, Guid referenceId)
    {
        return await _dbSet
            .Include(m => m.Product)
            .Where(m => m.ReferenceType == referenceType && m.ReferenceId == referenceId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetAvailableQuantityAsync(Guid productId)
    {
        var movements = await _dbSet
            .Where(m => m.ProductId == productId && !m.IsDeleted)
            .ToListAsync();

        return movements.Sum(m => m.MovementType switch
        {
            InventoryMovementType.Purchase or 
            InventoryMovementType.Return or 
            InventoryMovementType.TransferIn or 
            InventoryMovementType.InitialStock => m.Quantity,
            InventoryMovementType.Sale or 
            InventoryMovementType.TransferOut or 
            InventoryMovementType.Damage or 
            InventoryMovementType.Expired => -m.Quantity,
            InventoryMovementType.Adjustment => m.Quantity,
            _ => 0
        });
    }
}

