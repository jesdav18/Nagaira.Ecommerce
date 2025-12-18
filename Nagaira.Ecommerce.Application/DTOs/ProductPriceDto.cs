using System.ComponentModel.DataAnnotations;

namespace Nagaira.Ecommerce.Application.DTOs;

public record ProductPriceDto(
    Guid Id,
    Guid ProductId,
    Guid PriceLevelId,
    string PriceLevelName,
    decimal Price,
    decimal PriceWithoutTax,
    int MinQuantity,
    bool IsActive
);

public record CreateProductPriceDto(
    [Required(ErrorMessage = "El producto es requerido")]
    Guid ProductId,
    
    [Required(ErrorMessage = "El nivel de precio es requerido")]
    Guid PriceLevelId,
    
    [Required(ErrorMessage = "El precio es requerido")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    decimal Price,
    
    [Required(ErrorMessage = "El precio sin impuesto es requerido")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio sin impuesto debe ser mayor a 0")]
    decimal PriceWithoutTax,
    
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad mínima debe ser mayor a 0")]
    int MinQuantity
);

public record UpdateProductPriceDto(
    Guid Id,
    decimal Price,
    decimal PriceWithoutTax,
    int MinQuantity,
    bool IsActive
);

