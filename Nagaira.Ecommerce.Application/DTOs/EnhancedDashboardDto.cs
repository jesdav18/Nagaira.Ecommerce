namespace Nagaira.Ecommerce.Application.DTOs;

public record EnhancedDashboardDto(
    int TotalProducts,
    int ActiveProducts,
    int TotalOrders,
    int PendingOrders,
    int ProcessingOrders,
    int ShippedOrders,
    int DeliveredOrders,
    int CancelledOrders,
    decimal TotalRevenue,
    decimal MonthlyRevenue,
    decimal WeeklyRevenue,
    decimal DailyRevenue,
    int LowStockProducts,
    int ActiveOffers,
    int TotalCustomers,
    int NewCustomersThisMonth,
    List<TopProductDto> TopSellingProducts,
    List<SalesByPeriodDto> SalesByPeriod,
    decimal AverageOrderValue,
    int OrdersToday
);

public record TopProductDto(
    Guid ProductId,
    string ProductName,
    int TotalSold,
    decimal TotalRevenue
);

public record SalesByPeriodDto(
    string Period,
    decimal Revenue,
    int OrderCount
);

