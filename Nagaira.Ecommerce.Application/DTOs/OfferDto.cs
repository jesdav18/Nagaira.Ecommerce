using System.ComponentModel.DataAnnotations;

namespace Nagaira.Ecommerce.Application.DTOs;

public record OfferDto(
    Guid Id,
    string Name,
    string? Description,
    string OfferType,
    string Status,
    decimal? DiscountPercentage,
    decimal? DiscountAmount,
    decimal? MinPurchaseAmount,
    int? MinQuantity,
    int? MaxUsesPerCustomer,
    int? TotalMaxUses,
    int CurrentUses,
    DateTime StartDate,
    DateTime EndDate,
    int Priority,
    bool IsActive,
    List<Guid> ProductIds,
    List<Guid> CategoryIds
);

public record CreateOfferDto(
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(255, ErrorMessage = "El nombre no puede exceder 255 caracteres")]
    string Name,
    
    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    string? Description,
    
    [Required(ErrorMessage = "El tipo de oferta es requerido")]
    string OfferType,
    
    [Range(0, 100, ErrorMessage = "El porcentaje de descuento debe estar entre 0 y 100")]
    decimal? DiscountPercentage,
    
    [Range(0, double.MaxValue, ErrorMessage = "El monto de descuento debe ser mayor o igual a 0")]
    decimal? DiscountAmount,
    
    [Range(0, double.MaxValue, ErrorMessage = "El monto mínimo de compra debe ser mayor o igual a 0")]
    decimal? MinPurchaseAmount,
    
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad mínima debe ser mayor a 0")]
    int? MinQuantity,
    
    [Range(1, int.MaxValue, ErrorMessage = "El máximo de usos por cliente debe ser mayor a 0")]
    int? MaxUsesPerCustomer,
    
    [Range(1, int.MaxValue, ErrorMessage = "El máximo total de usos debe ser mayor a 0")]
    int? TotalMaxUses,
    
    [Required(ErrorMessage = "La fecha de inicio es requerida")]
    DateTime StartDate,
    
    [Required(ErrorMessage = "La fecha de fin es requerida")]
    DateTime EndDate,
    
    [Range(0, int.MaxValue, ErrorMessage = "La prioridad debe ser mayor o igual a 0")]
    int Priority,
    
    List<Guid>? ProductIds,
    List<Guid>? CategoryIds
);

public record UpdateOfferDto(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    decimal? DiscountPercentage,
    decimal? DiscountAmount,
    decimal? MinPurchaseAmount,
    int? MinQuantity,
    int? MaxUsesPerCustomer,
    int? TotalMaxUses,
    DateTime StartDate,
    DateTime EndDate,
    int Priority,
    bool IsActive,
    List<Guid>? ProductIds,
    List<Guid>? CategoryIds
);

public record OfferApplicationDto(
    Guid Id,
    Guid OfferId,
    string OfferName,
    Guid? OrderId,
    Guid? ProductId,
    decimal DiscountAmount,
    DateTime AppliedAt
);

