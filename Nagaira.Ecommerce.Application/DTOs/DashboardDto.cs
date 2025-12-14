namespace Nagaira.Ecommerce.Application.DTOs;

public record DashboardDto(
    int TotalProducts,
    int ActiveProducts,
    int TotalOrders,
    int PendingOrders,
    decimal TotalRevenue,
    decimal MonthlyRevenue,
    int LowStockProducts,
    int ActiveOffers,
    int TotalCustomers
);

