using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class ProductSupplierRepository : Repository<ProductSupplier>, IProductSupplierRepository
{
    public ProductSupplierRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ProductSupplier>> GetByProductIdAsync(Guid productId)
    {
        return await _dbSet
            .Include(ps => ps.Supplier)
            .Include(ps => ps.Product)
            .Where(ps => ps.ProductId == productId && !ps.IsDeleted)
            .OrderBy(ps => ps.Priority)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductSupplier>> GetBySupplierIdAsync(Guid supplierId)
    {
        return await _dbSet
            .Include(ps => ps.Product)
            .Include(ps => ps.Supplier)
            .Where(ps => ps.SupplierId == supplierId && !ps.IsDeleted)
            .ToListAsync();
    }

    public async Task<ProductSupplier?> GetPrimarySupplierByProductIdAsync(Guid productId)
    {
        return await _dbSet
            .Include(ps => ps.Supplier)
            .Include(ps => ps.Product)
            .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.IsPrimary && !ps.IsDeleted);
    }

    public async Task<ProductSupplier?> GetByProductAndSupplierAsync(Guid productId, Guid supplierId)
    {
        return await _dbSet
            .Include(ps => ps.Supplier)
            .Include(ps => ps.Product)
            .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.SupplierId == supplierId && !ps.IsDeleted);
    }

    public async Task<IEnumerable<ProductSupplier>> GetOrderedByPriorityAsync(Guid productId)
    {
        return await _dbSet
            .Include(ps => ps.Supplier)
            .Where(ps => ps.ProductId == productId && ps.IsActive && !ps.IsDeleted && ps.Supplier.IsActive)
            .OrderBy(ps => ps.Priority)
            .ToListAsync();
    }

    public async Task<decimal?> GetBestSupplierCostAsync(Guid productId)
    {
        var primarySupplier = await GetPrimarySupplierByProductIdAsync(productId);
        if (primarySupplier != null && primarySupplier.IsActive && !primarySupplier.IsDeleted)
        {
            return primarySupplier.SupplierCost;
        }

        var suppliers = await GetOrderedByPriorityAsync(productId);
        return suppliers.FirstOrDefault()?.SupplierCost;
    }
}

