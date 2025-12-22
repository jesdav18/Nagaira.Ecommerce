using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface ISupplierService
{
    Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync();
    Task<IEnumerable<SupplierDto>> GetActiveSuppliersAsync();
    Task<SupplierDto?> GetSupplierByIdAsync(Guid id);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto);
    Task UpdateSupplierAsync(UpdateSupplierDto dto);
    Task DeleteSupplierAsync(Guid id);
    Task ActivateSupplierAsync(Guid id);
    Task DeactivateSupplierAsync(Guid id);
}

