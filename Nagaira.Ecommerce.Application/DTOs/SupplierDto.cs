using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Nagaira.Ecommerce.Application.DTOs;

public record SupplierDto(
    Guid Id,
    string Name,
    string? LegalName,
    string? TaxId,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    string? Website,
    string? Notes,
    string? PaymentTerms,
    int LeadTimeDays,
    decimal? MinOrderAmount,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateSupplierDto(
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(255, ErrorMessage = "El nombre no puede exceder 255 caracteres")]
    string Name,
    
    [StringLength(255, ErrorMessage = "La razón social no puede exceder 255 caracteres")]
    string? LegalName,
    
    [StringLength(50, ErrorMessage = "El RUC/NIT no puede exceder 50 caracteres")]
    string? TaxId,
    
    [StringLength(255, ErrorMessage = "El nombre de contacto no puede exceder 255 caracteres")]
    string? ContactName,
    
    [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
    [RegularExpression(@"^$|^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email inválido")]
    string? Email,
    
    [StringLength(50, ErrorMessage = "El teléfono no puede exceder 50 caracteres")]
    string? Phone,
    
    string? Address,
    
    [StringLength(100, ErrorMessage = "La ciudad no puede exceder 100 caracteres")]
    string? City,
    
    [StringLength(100, ErrorMessage = "El estado no puede exceder 100 caracteres")]
    string? State,
    
    [StringLength(100, ErrorMessage = "El país no puede exceder 100 caracteres")]
    string? Country,
    
    [StringLength(20, ErrorMessage = "El código postal no puede exceder 20 caracteres")]
    string? PostalCode,
    
    [StringLength(500, ErrorMessage = "La URL no puede exceder 500 caracteres")]
    [RegularExpression(@"^$|^https?://.+", ErrorMessage = "URL inválida. Debe comenzar con http:// o https://")]
    string? Website,
    
    string? Notes,
    
    [StringLength(100, ErrorMessage = "Los términos de pago no pueden exceder 100 caracteres")]
    string? PaymentTerms,
    
    [Range(0, 365, ErrorMessage = "El tiempo de entrega debe estar entre 0 y 365 días")]
    int LeadTimeDays,
    
    [Range(0, double.MaxValue, ErrorMessage = "El monto mínimo debe ser mayor o igual a 0")]
    decimal? MinOrderAmount,
    
    bool IsActive
);

public record UpdateSupplierDto(
    Guid Id,
    
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(255, ErrorMessage = "El nombre no puede exceder 255 caracteres")]
    string Name,
    
    [StringLength(255, ErrorMessage = "La razón social no puede exceder 255 caracteres")]
    string? LegalName,
    
    [StringLength(50, ErrorMessage = "El RUC/NIT no puede exceder 50 caracteres")]
    string? TaxId,
    
    [StringLength(255, ErrorMessage = "El nombre de contacto no puede exceder 255 caracteres")]
    string? ContactName,
    
    [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
    [RegularExpression(@"^$|^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email inválido")]
    string? Email,
    
    [StringLength(50, ErrorMessage = "El teléfono no puede exceder 50 caracteres")]
    string? Phone,
    
    string? Address,
    
    [StringLength(100, ErrorMessage = "La ciudad no puede exceder 100 caracteres")]
    string? City,
    
    [StringLength(100, ErrorMessage = "El estado no puede exceder 100 caracteres")]
    string? State,
    
    [StringLength(100, ErrorMessage = "El país no puede exceder 100 caracteres")]
    string? Country,
    
    [StringLength(20, ErrorMessage = "El código postal no puede exceder 20 caracteres")]
    string? PostalCode,
    
    [StringLength(500, ErrorMessage = "La URL no puede exceder 500 caracteres")]
    [RegularExpression(@"^$|^https?://.+", ErrorMessage = "URL inválida. Debe comenzar con http:// o https://")]
    string? Website,
    
    string? Notes,
    
    [StringLength(100, ErrorMessage = "Los términos de pago no pueden exceder 100 caracteres")]
    string? PaymentTerms,
    
    [Range(0, 365, ErrorMessage = "El tiempo de entrega debe estar entre 0 y 365 días")]
    int LeadTimeDays,
    
    [Range(0, double.MaxValue, ErrorMessage = "El monto mínimo debe ser mayor o igual a 0")]
    decimal? MinOrderAmount,
    
    bool IsActive
);

