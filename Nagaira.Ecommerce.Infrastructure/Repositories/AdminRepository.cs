using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly ApplicationDbContext _context;

    public AdminRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetTotalProductsCountAsync()
    {
        return await _context.Products.CountAsync(p => !p.IsDeleted);
    }

    public async Task<int> GetActiveProductsCountAsync()
    {
        return await _context.Products.CountAsync(p => p.IsActive && !p.IsDeleted);
    }

    public async Task<int> GetTotalOrdersCountAsync()
    {
        return await _context.Orders.CountAsync(o => !o.IsDeleted);
    }

    public async Task<int> GetOrdersCountByStatusAsync(OrderStatus status)
    {
        return await _context.Orders.CountAsync(o => o.Status == status && !o.IsDeleted);
    }

    public async Task<decimal> GetTotalRevenueAsync()
    {
        return await _context.Orders
            .Where(o => !o.IsDeleted && o.Status == OrderStatus.Delivered)
            .SumAsync(o => (decimal?)o.Total) ?? 0;
    }

    public async Task<decimal> GetRevenueByPeriodAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Orders
            .Where(o => !o.IsDeleted 
                && o.Status == OrderStatus.Delivered
                && o.CreatedAt >= startDate
                && o.CreatedAt <= endDate)
            .SumAsync(o => (decimal?)o.Total) ?? 0;
    }

    public async Task<int> GetLowStockProductsCountAsync(int threshold)
    {
        return await _context.InventoryBalances
            .CountAsync(b => b.AvailableQuantity <= threshold && b.AvailableQuantity >= 0);
    }

    public async Task<int> GetActiveOffersCountAsync()
    {
        return await _context.Offers
            .CountAsync(o => o.IsActive 
                && o.Status == OfferStatus.Active
                && o.StartDate <= DateTime.UtcNow
                && o.EndDate >= DateTime.UtcNow
                && !o.IsDeleted);
    }

    public async Task<int> GetTotalCustomersCountAsync()
    {
        return await _context.Users
            .CountAsync(u => u.Role == UserRole.Customer && !u.IsDeleted);
    }

    public async Task<int> GetNewCustomersCountAsync(DateTime startDate)
    {
        return await _context.Users
            .CountAsync(u => u.Role == UserRole.Customer 
                && !u.IsDeleted 
                && u.CreatedAt >= startDate);
    }

    public async Task<int> GetOrdersCountTodayAsync()
    {
        return await _context.Orders
            .CountAsync(o => !o.IsDeleted && o.CreatedAt >= DateTime.UtcNow.Date);
    }

    public async Task<List<(Guid ProductId, string ProductName, int TotalSold, decimal Revenue)>> GetTopSellingProductsAsync(int top)
    {
        return await _context.OrderItems
            .Include(oi => oi.Product)
            .Where(oi => !oi.IsDeleted && oi.Order.Status == OrderStatus.Delivered && !oi.Order.IsDeleted)
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
            .Select(g => new { g.Key.ProductId, g.Key.Name, TotalSold = g.Sum(oi => oi.Quantity), Revenue = g.Sum(oi => oi.Subtotal) })
            .OrderByDescending(x => x.TotalSold)
            .Take(top)
            .Select(x => new ValueTuple<Guid, string, int, decimal>(x.ProductId, x.Name, x.TotalSold, x.Revenue))
            .ToListAsync();
    }

    public async Task<List<(string Period, decimal Revenue, int OrderCount)>> GetSalesByPeriodAsync(int days)
    {
        var result = new List<(string, decimal, int)>();
        for (int i = days - 1; i >= 0; i--)
        {
            var date = DateTime.UtcNow.AddDays(-i).Date;
            var revenue = await _context.Orders
                .Where(o => !o.IsDeleted 
                    && o.Status == OrderStatus.Delivered
                    && o.CreatedAt >= date 
                    && o.CreatedAt < date.AddDays(1))
                .SumAsync(o => (decimal?)o.Total) ?? 0;
            
            var orderCount = await _context.Orders
                .CountAsync(o => !o.IsDeleted 
                    && o.CreatedAt >= date 
                    && o.CreatedAt < date.AddDays(1));
            
            result.Add((date.ToString("yyyy-MM-dd"), revenue, orderCount));
        }
        return result;
    }

    public async Task<decimal> GetAverageOrderValueAsync()
    {
        var count = await _context.Orders
            .CountAsync(o => !o.IsDeleted && o.Status == OrderStatus.Delivered);
        
        if (count == 0) return 0;
        
        return await _context.Orders
            .Where(o => !o.IsDeleted && o.Status == OrderStatus.Delivered)
            .AverageAsync(o => (decimal?)o.Total) ?? 0;
    }

    public async Task<(int TotalCount, List<Product> Products)> GetProductsPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Prices)
                .ThenInclude(pp => pp.PriceLevel)
            .Include(p => p.InventoryBalance)
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm) || p.Sku.Contains(searchTerm));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (totalCount, products);
    }

    public async Task<(int TotalCount, List<Offer> Offers)> GetOffersPagedAsync(int pageNumber, int pageSize, string? status = null)
    {
        var query = _context.Offers
            .Include(o => o.Products)
            .Include(o => o.Categories)
            .Where(o => !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OfferStatus>(status, true, out var offerStatus))
        {
            query = query.Where(o => o.Status == offerStatus);
        }

        var totalCount = await query.CountAsync();
        var offers = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (totalCount, offers);
    }

    public async Task<(int TotalCount, List<InventoryMovement> Movements)> GetMovementsPagedAsync(int pageNumber, int pageSize, Guid? productId = null)
    {
        var query = _context.InventoryMovements
            .Include(m => m.Product)
            .Include(m => m.Creator)
            .Where(m => !m.IsDeleted);

        if (productId.HasValue)
        {
            query = query.Where(m => m.ProductId == productId.Value);
        }

        var totalCount = await query.CountAsync();
        var movements = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (totalCount, movements);
    }

    public async Task<List<Order>> GetDeliveredOrdersByPeriodAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => !o.IsDeleted 
                && o.Status == OrderStatus.Delivered
                && o.CreatedAt >= startDate 
                && o.CreatedAt <= endDate)
            .ToListAsync();
    }

    public async Task<List<(Guid ProductId, string ProductName, int QuantitySold, decimal Revenue)>> GetSalesByProductAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.OrderItems
            .Include(oi => oi.Product)
            .Where(oi => !oi.IsDeleted 
                && oi.Order.Status == OrderStatus.Delivered
                && !oi.Order.IsDeleted
                && oi.Order.CreatedAt >= startDate
                && oi.Order.CreatedAt <= endDate)
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
            .Select(g => new ValueTuple<Guid, string, int, decimal>(
                g.Key.ProductId,
                g.Key.Name,
                g.Sum(oi => oi.Quantity),
                g.Sum(oi => oi.Subtotal)
            ))
            .OrderByDescending(x => x.Item4)
            .ToListAsync();
    }

    public async Task<List<(Guid CategoryId, string CategoryName, int QuantitySold, decimal Revenue)>> GetSalesByCategoryAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.OrderItems
            .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
            .Where(oi => !oi.IsDeleted 
                && oi.Order.Status == OrderStatus.Delivered
                && !oi.Order.IsDeleted
                && oi.Order.CreatedAt >= startDate
                && oi.Order.CreatedAt <= endDate)
            .GroupBy(oi => new { oi.Product.CategoryId, oi.Product.Category.Name })
            .Select(g => new ValueTuple<Guid, string, int, decimal>(
                g.Key.CategoryId,
                g.Key.Name,
                g.Sum(oi => oi.Quantity),
                g.Sum(oi => oi.Subtotal)
            ))
            .OrderByDescending(x => x.Item4)
            .ToListAsync();
    }

    public async Task<List<(DateTime Date, decimal Revenue, int OrderCount)>> GetSalesByDayAsync(DateTime startDate, DateTime endDate)
    {
        var result = new List<(DateTime, decimal, int)>();
        var currentDate = startDate.Date;
        while (currentDate <= endDate.Date)
        {
            var revenue = await _context.Orders
                .Where(o => !o.IsDeleted 
                    && o.Status == OrderStatus.Delivered
                    && o.CreatedAt >= currentDate 
                    && o.CreatedAt < currentDate.AddDays(1))
                .SumAsync(o => (decimal?)o.Total) ?? 0;
            
            var orderCount = await _context.Orders
                .CountAsync(o => !o.IsDeleted 
                    && o.CreatedAt >= currentDate 
                    && o.CreatedAt < currentDate.AddDays(1));
            
            result.Add((currentDate, revenue, orderCount));
            currentDate = currentDate.AddDays(1);
        }
        return result;
    }

    public async Task<List<(Guid ProductId, string ProductName, string Sku, int AvailableQuantity, decimal? AverageCost, decimal? TotalValue)>> GetInventoryItemsAsync()
    {
        var products = await _context.Products
            .Include(p => p.InventoryBalance)
            .Where(p => !p.IsDeleted)
            .ToListAsync();

        var result = new List<(Guid, string, string, int, decimal?, decimal?)>();
        foreach (var product in products)
        {
            var balance = product.InventoryBalance;
            var availableQuantity = balance?.AvailableQuantity ?? 0;

            var averageCost = await _context.InventoryMovements
                .Where(m => m.ProductId == product.Id 
                    && m.MovementType == InventoryMovementType.Purchase
                    && !m.IsDeleted
                    && m.CostPerUnit.HasValue)
                .AverageAsync(m => (decimal?)m.CostPerUnit) ?? 0;

            var totalValue = averageCost * availableQuantity;

            result.Add((product.Id, product.Name, product.Sku, availableQuantity, averageCost > 0 ? averageCost : null, totalValue));
        }
        return result;
    }
}

