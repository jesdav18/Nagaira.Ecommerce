using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IMetaProductSyncStateRepository : IRepository<MetaProductSyncState>
{
    Task<MetaProductSyncState?> GetByProductIdAsync(Guid productId);
    Task<MetaProductSyncState> MarkPendingAsync(Guid productId, string retailerId);
    Task<bool> TryAcquireProductLockAsync(Guid productId, Guid lockId, DateTime lockedUntilAt);
}
