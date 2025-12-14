using System.ComponentModel.DataAnnotations;

namespace Nagaira.Ecommerce.Application.DTOs;

public record InventoryMovementDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string MovementType,
    int Quantity,
    string? ReferenceNumber,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Notes,
    decimal? CostPerUnit,
    decimal? TotalCost,
    Guid? CreatedBy,
    string? CreatedByName,
    DateTime CreatedAt
);

public record CreateInventoryMovementDto(
    [Required(ErrorMessage = "El producto es requerido")]
    Guid ProductId,
    
    [Required(ErrorMessage = "El tipo de movimiento es requerido")]
    string MovementType,
    
    [Required(ErrorMessage = "La cantidad es requerida")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
    int Quantity,
    
    [StringLength(100, ErrorMessage = "El número de referencia no puede exceder 100 caracteres")]
    string? ReferenceNumber,
    
    [StringLength(50, ErrorMessage = "El tipo de referencia no puede exceder 50 caracteres")]
    string? ReferenceType,
    
    Guid? ReferenceId,
    
    [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
    string? Notes,
    
    [Range(0, double.MaxValue, ErrorMessage = "El costo por unidad debe ser mayor o igual a 0")]
    decimal? CostPerUnit
);

public record InventoryBalanceDto(
    Guid ProductId,
    string ProductName,
    int AvailableQuantity,
    int ReservedQuantity,
    DateTime LastUpdatedAt
);

