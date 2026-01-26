using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardDto> GetDashboardStatsAsync()
    {
        var totalProducts = await _unitOfWork.Admin.GetTotalProductsCountAsync();
        var activeProducts = await _unitOfWork.Admin.GetActiveProductsCountAsync();
        var totalOrders = await _unitOfWork.Admin.GetTotalOrdersCountAsync();
        var pendingOrders = await _unitOfWork.Admin.GetOrdersCountByStatusAsync(OrderStatus.Pending);
        var totalRevenue = await _unitOfWork.Admin.GetTotalRevenueAsync();
        var monthlyRevenue = await _unitOfWork.Admin.GetRevenueByPeriodAsync(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
        var lowStockProducts = await _unitOfWork.Admin.GetLowStockProductsCountAsync(10);
        var activeOffers = await _unitOfWork.Admin.GetActiveOffersCountAsync();
        var totalCustomers = await _unitOfWork.Admin.GetTotalCustomersCountAsync();

        return new DashboardDto(
            totalProducts,
            activeProducts,
            totalOrders,
            pendingOrders,
            totalRevenue,
            monthlyRevenue,
            lowStockProducts,
            activeOffers,
            totalCustomers
        );
    }

    public async Task<EnhancedDashboardDto> GetEnhancedDashboardStatsAsync()
    {
        var totalProducts = await _unitOfWork.Admin.GetTotalProductsCountAsync();
        var activeProducts = await _unitOfWork.Admin.GetActiveProductsCountAsync();
        var totalOrders = await _unitOfWork.Admin.GetTotalOrdersCountAsync();
        var pendingOrders = await _unitOfWork.Admin.GetOrdersCountByStatusAsync(OrderStatus.Pending);
        var processingOrders = await _unitOfWork.Admin.GetOrdersCountByStatusAsync(OrderStatus.Processing);
        var shippedOrders = await _unitOfWork.Admin.GetOrdersCountByStatusAsync(OrderStatus.Shipped);
        var deliveredOrders = await _unitOfWork.Admin.GetOrdersCountByStatusAsync(OrderStatus.Delivered);
        var cancelledOrders = await _unitOfWork.Admin.GetOrdersCountByStatusAsync(OrderStatus.Cancelled);
        
        var totalRevenue = await _unitOfWork.Admin.GetTotalRevenueAsync();
        var monthlyRevenue = await _unitOfWork.Admin.GetRevenueByPeriodAsync(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
        var weeklyRevenue = await _unitOfWork.Admin.GetRevenueByPeriodAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        var dailyRevenue = await _unitOfWork.Admin.GetRevenueByPeriodAsync(DateTime.UtcNow.Date, DateTime.UtcNow);
        var ordersToday = await _unitOfWork.Admin.GetOrdersCountTodayAsync();
        var lowStockProducts = await _unitOfWork.Admin.GetLowStockProductsCountAsync(10);
        var activeOffers = await _unitOfWork.Admin.GetActiveOffersCountAsync();
        var totalCustomers = await _unitOfWork.Admin.GetTotalCustomersCountAsync();
        var newCustomersThisMonth = await _unitOfWork.Admin.GetNewCustomersCountAsync(DateTime.UtcNow.AddMonths(-1));

        var topSellingData = await _unitOfWork.Admin.GetTopSellingProductsAsync(10);
        var topSellingProducts = topSellingData.Select(t => new TopProductDto(
            t.ProductId,
            t.ProductName,
            t.TotalSold,
            t.Revenue
        )).ToList();

        var salesByPeriodData = await _unitOfWork.Admin.GetSalesByPeriodAsync(7);
        var salesByPeriod = salesByPeriodData.Select(s => new SalesByPeriodDto(
            s.Period,
            s.Revenue,
            s.OrderCount
        )).ToList();

        var averageOrderValue = await _unitOfWork.Admin.GetAverageOrderValueAsync();

        return new EnhancedDashboardDto(
            totalProducts,
            activeProducts,
            totalOrders,
            pendingOrders,
            processingOrders,
            shippedOrders,
            deliveredOrders,
            cancelledOrders,
            totalRevenue,
            monthlyRevenue,
            weeklyRevenue,
            dailyRevenue,
            lowStockProducts,
            activeOffers,
            totalCustomers,
            newCustomersThisMonth,
            topSellingProducts,
            salesByPeriod,
            averageOrderValue,
            ordersToday
        );
    }

    public async Task<PagedResultDto<ProductDto>> GetProductsPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null, Guid? categoryId = null, bool? isFeatured = null)
    {
        var (totalCount, products) = await _unitOfWork.Admin.GetProductsPagedAsync(pageNumber, pageSize, searchTerm, isActive, categoryId, isFeatured);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var productDtos = products.Select(p => new ProductDto(
            p.Id,
            p.Name,
            p.Description,
            p.Sku,
            p.Slug,
            p.IsActive,
            p.CategoryId,
            p.Category?.Name ?? string.Empty,
            p.InventoryBalance?.AvailableQuantity ?? 0,
            p.InventoryBalance?.ReservedQuantity ?? 0,
            p.Cost,
            p.HasVirtualStock,
            p.IsFeatured,
            p.Images.Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.AltText, i.IsPrimary, i.DisplayOrder)).ToList(),
            p.Prices.Select(pp => new ProductPriceDto(
                pp.Id,
                pp.ProductId,
                pp.PriceLevelId,
                pp.PriceLevel?.Name ?? string.Empty,
                pp.Price,
                pp.PriceWithoutTax,
                pp.MinQuantity,
                pp.IsActive
            )).ToList()
        ));

        return new PagedResultDto<ProductDto>(
            productDtos,
            totalCount,
            pageNumber,
            pageSize,
            totalPages
        );
    }

    public async Task<PagedResultDto<OfferDto>> GetOffersPagedAsync(int pageNumber, int pageSize, string? status = null)
    {
        var (totalCount, offers) = await _unitOfWork.Admin.GetOffersPagedAsync(pageNumber, pageSize, status);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var offerDtos = offers.Select(o => new OfferDto(
            o.Id,
            o.Name,
            o.Description,
            o.OfferType.ToString(),
            o.Status.ToString(),
            o.DiscountPercentage,
            o.DiscountAmount,
            o.MinPurchaseAmount,
            o.MinQuantity,
            o.MaxUsesPerCustomer,
            o.TotalMaxUses,
            o.CurrentUses,
            o.StartDate,
            o.EndDate,
            o.Priority,
            o.IsActive,
            o.Products.Where(p => !p.IsDeleted).Select(p => p.ProductId).ToList(),
            o.Categories.Where(c => !c.IsDeleted).Select(c => c.CategoryId).ToList()
        ));

        return new PagedResultDto<OfferDto>(
            offerDtos,
            totalCount,
            pageNumber,
            pageSize,
            totalPages
        );
    }

    public async Task<PagedResultDto<InventoryMovementDto>> GetMovementsPagedAsync(int pageNumber, int pageSize, Guid? productId = null)
    {
        var (totalCount, movements) = await _unitOfWork.Admin.GetMovementsPagedAsync(pageNumber, pageSize, productId);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var movementDtos = movements.Select(m => new InventoryMovementDto(
            m.Id,
            m.ProductId,
            m.Product?.Name ?? string.Empty,
            m.MovementType.ToString(),
            m.Quantity,
            m.ReferenceNumber,
            m.ReferenceType,
            m.ReferenceId,
            m.Notes,
            m.CostPerUnit,
            m.TotalCost,
            m.CreatedBy,
            m.Creator != null ? $"{m.Creator.FirstName} {m.Creator.LastName}" : null,
            m.CreatedAt
        ));

        return new PagedResultDto<InventoryMovementDto>(
            movementDtos,
            totalCount,
            pageNumber,
            pageSize,
            totalPages
        );
    }
}

