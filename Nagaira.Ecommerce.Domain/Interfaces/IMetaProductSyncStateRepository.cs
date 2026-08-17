using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IMetaProductSyncStateRepository : IRepository<MetaProductSyncState>
{
    Task<MetaProductSyncState?> GetByProductIdAsync(Guid productId);
    Task<IReadOnlyList<MetaProductSyncState>> GetByProductIdsAsync(IEnumerable<Guid> productIds);
    Task<IReadOnlyList<MetaProductSyncState>> GetProcessingWithBatchHandleAsync(int limit);
    Task<MetaProductSyncState> MarkPendingAsync(Guid productId, string retailerId);
    Task<bool> TryAcquireProductLockAsync(Guid productId, Guid lockId, DateTime lockedUntilAt);
}
