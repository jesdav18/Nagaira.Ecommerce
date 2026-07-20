using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class MetaProductSyncStateRepository : Repository<MetaProductSyncState>, IMetaProductSyncStateRepository
{
    public MetaProductSyncStateRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<MetaProductSyncState?> GetByProductIdAsync(Guid productId)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.ProductId == productId);
    }

    public async Task<MetaProductSyncState> MarkPendingAsync(Guid productId, string retailerId)
    {
        var now = DateTime.UtcNow;
        var state = await GetByProductIdAsync(productId);
        if (state == null)
        {
            state = new MetaProductSyncState
            {
                ProductId = productId,
                RetailerId = retailerId,
                Status = MetaProductSyncStatuses.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };
            await AddAsync(state);
            return state;
        }

        state.RetailerId = retailerId;
        state.Status = MetaProductSyncStatuses.Pending;
        state.UpdatedAt = now;
        state.LastErrorCode = null;
        state.LastErrorMessage = null;
        state.LockId = null;
        state.LockedUntilAt = null;
        await UpdateAsync(state);
        return state;
    }

    public async Task<bool> TryAcquireProductLockAsync(Guid productId, Guid lockId, DateTime lockedUntilAt)
    {
        var now = DateTime.UtcNow;
        var updated = await _dbSet
            .Where(s => s.ProductId == productId
                && (s.Status == MetaProductSyncStatuses.Pending || s.Status == MetaProductSyncStatuses.Failed)
                && (s.LockedUntilAt == null || s.LockedUntilAt <= now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, MetaProductSyncStatuses.InProgress)
                .SetProperty(s => s.LockId, lockId)
                .SetProperty(s => s.LockedUntilAt, lockedUntilAt)
                .SetProperty(s => s.LastAttemptAt, now)
                .SetProperty(s => s.UpdatedAt, now));

        return updated == 1;
    }
}
