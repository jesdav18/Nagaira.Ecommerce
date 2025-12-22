using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IProductSupplierService
{
    Task<IEnumerable<ProductSupplierDto>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<ProductSupplierDto>> GetBySupplierIdAsync(Guid supplierId);
    Task<ProductSupplierDto?> GetPrimarySupplierByProductIdAsync(Guid productId);
    Task<ProductSupplierDto> CreateProductSupplierAsync(CreateProductSupplierDto dto);
    Task UpdateProductSupplierAsync(UpdateProductSupplierDto dto, Guid? userId = null, string? changeReason = null);
    Task<IEnumerable<SupplierCostHistoryDto>> GetCostHistoryAsync(Guid productSupplierId);
    Task DeleteProductSupplierAsync(Guid id);
    Task SetAsPrimaryAsync(Guid productId, Guid supplierId);
    Task<decimal?> GetBestSupplierCostAsync(Guid productId);
}

