using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class ProductSupplierService : IProductSupplierService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductSupplierService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProductSupplierDto>> GetByProductIdAsync(Guid productId)
    {
        var productSuppliers = await _unitOfWork.ProductSuppliers.GetByProductIdAsync(productId);
        return productSuppliers.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductSupplierDto>> GetBySupplierIdAsync(Guid supplierId)
    {
        var productSuppliers = await _unitOfWork.ProductSuppliers.GetBySupplierIdAsync(supplierId);
        return productSuppliers.Select(MapToDto);
    }

    public async Task<ProductSupplierDto?> GetPrimarySupplierByProductIdAsync(Guid productId)
    {
        var productSupplier = await _unitOfWork.ProductSuppliers.GetPrimarySupplierByProductIdAsync(productId);
        return productSupplier != null ? MapToDto(productSupplier) : null;
    }

    public async Task<ProductSupplierDto> CreateProductSupplierAsync(CreateProductSupplierDto dto)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
        if (product == null) throw new Exception("Producto no encontrado");

        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(dto.SupplierId);
        if (supplier == null) throw new Exception("Proveedor no encontrado");

        var existing = await _unitOfWork.ProductSuppliers.GetByProductAndSupplierAsync(dto.ProductId, dto.SupplierId);
        if (existing != null && !existing.IsDeleted)
            throw new Exception("Ya existe una relación entre este producto y proveedor");

        var existingPriority = await _unitOfWork.ProductSuppliers.GetByProductIdAsync(dto.ProductId);
        var priorityExists = existingPriority.Any(ps => ps.Priority == dto.Priority && !ps.IsDeleted && ps.Id != (existing?.Id ?? Guid.Empty));
        if (priorityExists)
            throw new Exception($"Ya existe un proveedor con prioridad {dto.Priority} para este producto");

        if (dto.IsPrimary)
        {
            var currentPrimary = await _unitOfWork.ProductSuppliers.GetPrimarySupplierByProductIdAsync(dto.ProductId);
            if (currentPrimary != null)
            {
                currentPrimary.IsPrimary = false;
                currentPrimary.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.ProductSuppliers.UpdateAsync(currentPrimary);
            }
        }

        var productSupplier = new ProductSupplier
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            SupplierId = dto.SupplierId,
            SupplierSku = dto.SupplierSku,
            SupplierCost = dto.SupplierCost,
            IsPrimary = dto.IsPrimary,
            Priority = dto.Priority,
            LeadTimeDays = dto.LeadTimeDays,
            MinOrderQuantity = dto.MinOrderQuantity,
            Notes = dto.Notes,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ProductSuppliers.AddAsync(productSupplier);
        await _unitOfWork.SaveChangesAsync();

        if (dto.IsPrimary)
        {
            product.Cost = dto.SupplierCost;
            product.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Products.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();
        }

        var created = await _unitOfWork.ProductSuppliers.GetByIdAsync(productSupplier.Id);
        return MapToDto(created!);
    }

    public async Task UpdateProductSupplierAsync(UpdateProductSupplierDto dto, Guid? userId = null, string? changeReason = null)
    {
        var productSupplier = await _unitOfWork.ProductSuppliers.GetByIdAsync(dto.Id);
        if (productSupplier == null || productSupplier.IsDeleted)
            throw new Exception("Relación producto-proveedor no encontrada");

        var existingPriority = await _unitOfWork.ProductSuppliers.GetByProductIdAsync(productSupplier.ProductId);
        var priorityExists = existingPriority.Any(ps => ps.Priority == dto.Priority && !ps.IsDeleted && ps.Id != dto.Id);
        if (priorityExists)
            throw new Exception($"Ya existe un proveedor con prioridad {dto.Priority} para este producto");

        var wasPrimary = productSupplier.IsPrimary;
        var oldCost = productSupplier.SupplierCost;
        var costChanged = oldCost != dto.SupplierCost;

        if (dto.IsPrimary && !wasPrimary)
        {
            var currentPrimary = await _unitOfWork.ProductSuppliers.GetPrimarySupplierByProductIdAsync(productSupplier.ProductId);
            if (currentPrimary != null && currentPrimary.Id != dto.Id)
            {
                currentPrimary.IsPrimary = false;
                currentPrimary.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.ProductSuppliers.UpdateAsync(currentPrimary);
            }
        }

        productSupplier.SupplierSku = dto.SupplierSku;
        productSupplier.SupplierCost = dto.SupplierCost;
        productSupplier.IsPrimary = dto.IsPrimary;
        productSupplier.Priority = dto.Priority;
        productSupplier.LeadTimeDays = dto.LeadTimeDays;
        productSupplier.MinOrderQuantity = dto.MinOrderQuantity;
        productSupplier.Notes = dto.Notes;
        productSupplier.IsActive = dto.IsActive;
        productSupplier.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ProductSuppliers.UpdateAsync(productSupplier);

        if (costChanged)
        {
            var costHistory = new SupplierCostHistory
            {
                Id = Guid.NewGuid(),
                ProductSupplierId = productSupplier.Id,
                OldCost = oldCost,
                NewCost = dto.SupplierCost,
                ChangedBy = userId,
                ChangeReason = changeReason,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<SupplierCostHistory>().AddAsync(costHistory);
        }

        if (dto.IsPrimary)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productSupplier.ProductId);
            if (product != null)
            {
                product.Cost = dto.SupplierCost;
                product.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Products.UpdateAsync(product);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteProductSupplierAsync(Guid id)
    {
        var productSupplier = await _unitOfWork.ProductSuppliers.GetByIdAsync(id);
        if (productSupplier == null) throw new Exception("Relación no encontrada");

        if (productSupplier.IsPrimary)
        {
            var otherSuppliers = await _unitOfWork.ProductSuppliers.GetOrderedByPriorityAsync(productSupplier.ProductId);
            var nextSupplier = otherSuppliers.FirstOrDefault(ps => ps.Id != id);
            
            if (nextSupplier != null)
            {
                nextSupplier.IsPrimary = true;
                nextSupplier.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.ProductSuppliers.UpdateAsync(nextSupplier);

                var product = await _unitOfWork.Products.GetByIdAsync(productSupplier.ProductId);
                if (product != null)
                {
                    product.Cost = nextSupplier.SupplierCost;
                    product.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Products.UpdateAsync(product);
                }
            }
        }

        await _unitOfWork.ProductSuppliers.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task SetAsPrimaryAsync(Guid productId, Guid supplierId)
    {
        var productSupplier = await _unitOfWork.ProductSuppliers.GetByProductAndSupplierAsync(productId, supplierId);
        if (productSupplier == null || productSupplier.IsDeleted)
            throw new Exception("Relación producto-proveedor no encontrada");

        var currentPrimary = await _unitOfWork.ProductSuppliers.GetPrimarySupplierByProductIdAsync(productId);
        if (currentPrimary != null && currentPrimary.Id != productSupplier.Id)
        {
            currentPrimary.IsPrimary = false;
            currentPrimary.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.ProductSuppliers.UpdateAsync(currentPrimary);
        }

        productSupplier.IsPrimary = true;
        productSupplier.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.ProductSuppliers.UpdateAsync(productSupplier);

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product != null)
        {
            product.Cost = productSupplier.SupplierCost;
            product.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Products.UpdateAsync(product);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<decimal?> GetBestSupplierCostAsync(Guid productId)
    {
        return await _unitOfWork.ProductSuppliers.GetBestSupplierCostAsync(productId);
    }

    public async Task<IEnumerable<SupplierCostHistoryDto>> GetCostHistoryAsync(Guid productSupplierId)
    {
        var history = await _unitOfWork.GetDbContext().Set<SupplierCostHistory>()
            .Include(h => h.ProductSupplier)
                .ThenInclude(ps => ps.Product)
            .Include(h => h.ProductSupplier)
                .ThenInclude(ps => ps.Supplier)
            .Include(h => h.ChangedByUser)
            .Where(h => h.ProductSupplierId == productSupplierId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return history.Select(h => new SupplierCostHistoryDto(
            h.Id,
            h.ProductSupplierId,
            h.ProductSupplier.Product?.Name ?? string.Empty,
            h.ProductSupplier.Supplier?.Name ?? string.Empty,
            h.OldCost,
            h.NewCost,
            h.ChangedByUser != null ? $"{h.ChangedByUser.FirstName} {h.ChangedByUser.LastName}" : null,
            h.ChangeReason,
            h.CreatedAt
        ));
    }

    private static ProductSupplierDto MapToDto(ProductSupplier ps)
    {
        return new ProductSupplierDto(
            ps.Id,
            ps.ProductId,
            ps.Product?.Name ?? string.Empty,
            ps.SupplierId,
            ps.Supplier?.Name ?? string.Empty,
            ps.SupplierSku,
            ps.SupplierCost,
            ps.IsPrimary,
            ps.Priority,
            ps.LeadTimeDays,
            ps.MinOrderQuantity,
            ps.Notes,
            ps.IsActive,
            ps.CreatedAt
        );
    }
}

