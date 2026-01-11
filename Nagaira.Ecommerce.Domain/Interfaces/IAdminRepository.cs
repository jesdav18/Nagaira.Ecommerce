using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IAdminRepository
{
    Task<int> GetTotalProductsCountAsync();
    Task<int> GetActiveProductsCountAsync();
    Task<int> GetTotalOrdersCountAsync();
    Task<int> GetOrdersCountByStatusAsync(OrderStatus status);
    Task<decimal> GetTotalRevenueAsync();
    Task<decimal> GetRevenueByPeriodAsync(DateTime startDate, DateTime endDate);
    Task<int> GetLowStockProductsCountAsync(int threshold);
    Task<int> GetActiveOffersCountAsync();
    Task<int> GetTotalCustomersCountAsync();
    Task<int> GetNewCustomersCountAsync(DateTime startDate);
    Task<int> GetOrdersCountTodayAsync();
    Task<List<(Guid ProductId, string ProductName, int TotalSold, decimal Revenue)>> GetTopSellingProductsAsync(int top);
    Task<List<(string Period, decimal Revenue, int OrderCount)>> GetSalesByPeriodAsync(int days);
    Task<decimal> GetAverageOrderValueAsync();
    Task<(int TotalCount, List<Product> Products)> GetProductsPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null, Guid? categoryId = null);
    Task<(int TotalCount, List<Offer> Offers)> GetOffersPagedAsync(int pageNumber, int pageSize, string? status = null);
    Task<(int TotalCount, List<InventoryMovement> Movements)> GetMovementsPagedAsync(int pageNumber, int pageSize, Guid? productId = null);
    Task<List<Order>> GetDeliveredOrdersByPeriodAsync(DateTime startDate, DateTime endDate);
    Task<List<(Guid ProductId, string ProductName, int QuantitySold, decimal Revenue)>> GetSalesByProductAsync(DateTime startDate, DateTime endDate);
    Task<List<(Guid CategoryId, string CategoryName, int QuantitySold, decimal Revenue)>> GetSalesByCategoryAsync(DateTime startDate, DateTime endDate);
    Task<List<(DateTime Date, decimal Revenue, int OrderCount)>> GetSalesByDayAsync(DateTime startDate, DateTime endDate);
    Task<List<(Guid ProductId, string ProductName, string Sku, int AvailableQuantity, decimal? AverageCost, decimal? TotalValue)>> GetInventoryItemsAsync();
}

