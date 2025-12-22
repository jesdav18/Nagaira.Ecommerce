using System.ComponentModel.DataAnnotations;

namespace Nagaira.Ecommerce.Application.DTOs;

public record ProductSupplierDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid SupplierId,
    string SupplierName,
    string? SupplierSku,
    decimal SupplierCost,
    bool IsPrimary,
    int Priority,
    int LeadTimeDays,
    int MinOrderQuantity,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateProductSupplierDto(
    [Required(ErrorMessage = "El producto es requerido")]
    Guid ProductId,
    
    [Required(ErrorMessage = "El proveedor es requerido")]
    Guid SupplierId,
    
    [StringLength(100, ErrorMessage = "El SKU del proveedor no puede exceder 100 caracteres")]
    string? SupplierSku,
    
    [Required(ErrorMessage = "El costo es requerido")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El costo debe ser mayor a 0")]
    decimal SupplierCost,
    
    bool IsPrimary,
    
    [Range(1, 100, ErrorMessage = "La prioridad debe estar entre 1 y 100")]
    int Priority,
    
    [Range(0, 365, ErrorMessage = "El tiempo de entrega debe estar entre 0 y 365 días")]
    int LeadTimeDays,
    
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad mínima debe ser mayor a 0")]
    int MinOrderQuantity,
    
    string? Notes,
    
    bool IsActive
);

public record UpdateProductSupplierDto(
    Guid Id,
    
    [StringLength(100, ErrorMessage = "El SKU del proveedor no puede exceder 100 caracteres")]
    string? SupplierSku,
    
    [Required(ErrorMessage = "El costo es requerido")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El costo debe ser mayor a 0")]
    decimal SupplierCost,
    
    bool IsPrimary,
    
    [Range(1, 100, ErrorMessage = "La prioridad debe estar entre 1 y 100")]
    int Priority,
    
    [Range(0, 365, ErrorMessage = "El tiempo de entrega debe estar entre 0 y 365 días")]
    int LeadTimeDays,
    
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad mínima debe ser mayor a 0")]
    int MinOrderQuantity,
    
    string? Notes,
    
    bool IsActive
);

