using System.ComponentModel.DataAnnotations;

namespace Nagaira.Ecommerce.Application.DTOs;

public record CreateProductDto(
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(255, ErrorMessage = "El nombre no puede exceder 255 caracteres")]
    string Name,
    
    [StringLength(2000, ErrorMessage = "La descripción no puede exceder 2000 caracteres")]
    string Description,
    
    [Required(ErrorMessage = "El SKU es requerido")]
    [StringLength(50, ErrorMessage = "El SKU no puede exceder 50 caracteres")]
    string Sku,
    
    [Required(ErrorMessage = "La categoría es requerida")]
    Guid CategoryId,
    
    [Range(0, double.MaxValue, ErrorMessage = "El costo debe ser mayor o igual a 0")]
    decimal? Cost,
    
    List<CreateProductPriceDto>? Prices,
    
    List<CreateProductImageDto>? Images
);

public record UpdateProductDto(
    [Required(ErrorMessage = "El ID es requerido")]
    Guid Id,
    
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(255, ErrorMessage = "El nombre no puede exceder 255 caracteres")]
    string Name,
    
    [StringLength(2000, ErrorMessage = "La descripción no puede exceder 2000 caracteres")]
    string Description,
    
    [Range(0, double.MaxValue, ErrorMessage = "El costo debe ser mayor o igual a 0")]
    decimal? Cost,
    
    bool IsActive
);

public record CreateProductImageDto(
    [Required(ErrorMessage = "El ID del producto es requerido")]
    Guid ProductId,
    [Required(ErrorMessage = "La URL de la imagen es requerida")]
    string ImageUrl,
    string AltText,
    bool IsPrimary,
    int DisplayOrder
);
