namespace Nagaira.Ecommerce.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    string Sku,
    string Slug,
    bool IsActive,
    Guid CategoryId,
    string CategoryName,
    int AvailableQuantity,
    int ReservedQuantity,
    decimal? Cost,
    bool HasVirtualStock,
    bool IsFeatured,
    List<ProductImageDto> Images,
    List<ProductPriceDto> Prices
);

public record ProductImageDto(
    Guid Id,
    string ImageUrl,
    string AltText,
    bool IsPrimary,
    int DisplayOrder
);

public record UpdateProductImageDto(
    Guid Id,
    string? ImageUrl,
    string? AltText,
    bool? IsPrimary,
    int? DisplayOrder
);
