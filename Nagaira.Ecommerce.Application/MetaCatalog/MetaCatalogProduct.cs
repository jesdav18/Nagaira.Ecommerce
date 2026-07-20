namespace Nagaira.Ecommerce.Application.MetaCatalog;

public record MetaCatalogProduct(
    string RetailerId,
    string Name,
    string Description,
    string Brand,
    string Availability,
    string Condition,
    string Price,
    string Currency,
    string Url,
    string ImageUrl,
    string? CategoryName,
    string? Sku
);
