namespace Nagaira.Ecommerce.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    string Sku,
    bool IsActive,
    Guid CategoryId,
    string CategoryName,
    int AvailableQuantity,
    int ReservedQuantity,
    decimal? Cost,
    bool HasVirtualStock,
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
