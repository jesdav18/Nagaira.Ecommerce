using System.ComponentModel.DataAnnotations;

namespace Nagaira.Ecommerce.Application.DTOs;

public record ProductPriceDto(
    Guid Id,
    Guid ProductId,
    Guid PriceLevelId,
    string PriceLevelName,
    decimal Price,
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
    
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad mínima debe ser mayor a 0")]
    int MinQuantity
);

public record UpdateProductPriceDto(
    Guid Id,
    decimal Price,
    int MinQuantity,
    bool IsActive
);

