using System.ComponentModel.DataAnnotations;

namespace Nagaira.Ecommerce.Application.DTOs;

public record PaymentMethodTypeDto(
    Guid Id,
    string Name,
    string Label,
    string? Description,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAt
);

public record CreatePaymentMethodTypeDto(
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    string Name,
    
    [Required(ErrorMessage = "La etiqueta es requerida")]
    [StringLength(255, ErrorMessage = "La etiqueta no puede exceder 255 caracteres")]
    string Label,
    
    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    string? Description,
    
    int DisplayOrder,
    
    bool IsActive
);

public record UpdatePaymentMethodTypeDto(
    Guid Id,
    
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    string Name,
    
    [Required(ErrorMessage = "La etiqueta es requerida")]
    [StringLength(255, ErrorMessage = "La etiqueta no puede exceder 255 caracteres")]
    string Label,
    
    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    string? Description,
    
    int DisplayOrder,
    
    bool IsActive
);

// DTO simplificado para el formulario de medios de pago (solo Value y Label)
public record PaymentMethodTypeSimpleDto(
    string Value,
    string Label
);
