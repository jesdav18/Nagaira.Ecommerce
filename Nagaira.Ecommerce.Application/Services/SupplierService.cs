using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupplierService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync()
    {
        var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
        return suppliers.Select(MapToDto);
    }

    public async Task<IEnumerable<SupplierDto>> GetActiveSuppliersAsync()
    {
        var suppliers = await _unitOfWork.Suppliers.GetActiveSuppliersAsync();
        return suppliers.Select(MapToDto);
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(Guid id)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
        return supplier != null ? MapToDto(supplier) : null;
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto)
    {
        var existing = await _unitOfWork.Suppliers.GetByNameAsync(dto.Name);
        if (existing != null && !existing.IsDeleted)
            throw new Exception($"Ya existe un proveedor con el nombre '{dto.Name}'");

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            LegalName = dto.LegalName,
            TaxId = dto.TaxId,
            ContactName = dto.ContactName,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Country = dto.Country,
            PostalCode = dto.PostalCode,
            Website = dto.Website,
            Notes = dto.Notes,
            PaymentTerms = dto.PaymentTerms,
            LeadTimeDays = dto.LeadTimeDays,
            MinOrderAmount = dto.MinOrderAmount,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Suppliers.AddAsync(supplier);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(supplier);
    }

    public async Task UpdateSupplierAsync(UpdateSupplierDto dto)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(dto.Id);
        if (supplier == null || supplier.IsDeleted)
            throw new Exception("Proveedor no encontrado");

        var existingWithName = await _unitOfWork.Suppliers.GetByNameAsync(dto.Name);
        if (existingWithName != null && existingWithName.Id != dto.Id && !existingWithName.IsDeleted)
            throw new Exception($"Ya existe otro proveedor con el nombre '{dto.Name}'");

        supplier.Name = dto.Name;
        supplier.LegalName = dto.LegalName;
        supplier.TaxId = dto.TaxId;
        supplier.ContactName = dto.ContactName;
        supplier.Email = dto.Email;
        supplier.Phone = dto.Phone;
        supplier.Address = dto.Address;
        supplier.City = dto.City;
        supplier.State = dto.State;
        supplier.Country = dto.Country;
        supplier.PostalCode = dto.PostalCode;
        supplier.Website = dto.Website;
        supplier.Notes = dto.Notes;
        supplier.PaymentTerms = dto.PaymentTerms;
        supplier.LeadTimeDays = dto.LeadTimeDays;
        supplier.MinOrderAmount = dto.MinOrderAmount;
        supplier.IsActive = dto.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Suppliers.UpdateAsync(supplier);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteSupplierAsync(Guid id)
    {
        await _unitOfWork.Suppliers.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ActivateSupplierAsync(Guid id)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
        if (supplier == null || supplier.IsDeleted)
            throw new Exception("Proveedor no encontrado");

        supplier.IsActive = true;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Suppliers.UpdateAsync(supplier);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivateSupplierAsync(Guid id)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
        if (supplier == null || supplier.IsDeleted)
            throw new Exception("Proveedor no encontrado");

        supplier.IsActive = false;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Suppliers.UpdateAsync(supplier);
        await _unitOfWork.SaveChangesAsync();
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto(
            supplier.Id,
            supplier.Name,
            supplier.LegalName,
            supplier.TaxId,
            supplier.ContactName,
            supplier.Email,
            supplier.Phone,
            supplier.Address,
            supplier.City,
            supplier.State,
            supplier.Country,
            supplier.PostalCode,
            supplier.Website,
            supplier.Notes,
            supplier.PaymentTerms,
            supplier.LeadTimeDays,
            supplier.MinOrderAmount,
            supplier.IsActive,
            supplier.CreatedAt
        );
    }
}

