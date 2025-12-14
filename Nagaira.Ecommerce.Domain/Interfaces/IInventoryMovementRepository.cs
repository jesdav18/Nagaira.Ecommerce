using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IInventoryMovementRepository : IRepository<InventoryMovement>
{
    Task<IEnumerable<InventoryMovement>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<InventoryMovement>> GetByReferenceAsync(string referenceType, Guid referenceId);
    Task<int> GetAvailableQuantityAsync(Guid productId);
}

