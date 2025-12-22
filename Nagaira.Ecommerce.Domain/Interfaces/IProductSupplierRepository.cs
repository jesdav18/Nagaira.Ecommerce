using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IProductSupplierRepository : IRepository<ProductSupplier>
{
    Task<IEnumerable<ProductSupplier>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<ProductSupplier>> GetBySupplierIdAsync(Guid supplierId);
    Task<ProductSupplier?> GetPrimarySupplierByProductIdAsync(Guid productId);
    Task<ProductSupplier?> GetByProductAndSupplierAsync(Guid productId, Guid supplierId);
    Task<IEnumerable<ProductSupplier>> GetOrderedByPriorityAsync(Guid productId);
    Task<decimal?> GetBestSupplierCostAsync(Guid productId);
}

