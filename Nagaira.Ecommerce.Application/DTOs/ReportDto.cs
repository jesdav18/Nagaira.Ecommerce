namespace Nagaira.Ecommerce.Application.DTOs;

public record SalesReportDto(
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalRevenue,
    int TotalOrders,
    decimal AverageOrderValue,
    List<SalesByProductDto> SalesByProduct,
    List<SalesByCategoryDto> SalesByCategory,
    List<SalesByDayDto> SalesByDay
);

public record SalesByProductDto(
    Guid ProductId,
    string ProductName,
    int QuantitySold,
    decimal Revenue
);

public record SalesByCategoryDto(
    Guid CategoryId,
    string CategoryName,
    int QuantitySold,
    decimal Revenue
);

public record SalesByDayDto(
    DateTime Date,
    decimal Revenue,
    int OrderCount
);

public record InventoryReportDto(
    DateTime GeneratedAt,
    int TotalProducts,
    int ProductsInStock,
    int ProductsOutOfStock,
    int ProductsLowStock,
    decimal TotalInventoryValue,
    List<InventoryItemDto> InventoryItems
);

public record InventoryItemDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    int AvailableQuantity,
    decimal? AverageCost,
    decimal? TotalValue
);

