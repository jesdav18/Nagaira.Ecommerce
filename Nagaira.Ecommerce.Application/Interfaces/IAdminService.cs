using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IAdminService
{
    Task<DashboardDto> GetDashboardStatsAsync();
    Task<EnhancedDashboardDto> GetEnhancedDashboardStatsAsync();
    Task<PagedResultDto<ProductDto>> GetProductsPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null, Guid? categoryId = null, bool? isFeatured = null);
    Task<PagedResultDto<OfferDto>> GetOffersPagedAsync(int pageNumber, int pageSize, string? status = null);
    Task<PagedResultDto<InventoryMovementDto>> GetMovementsPagedAsync(int pageNumber, int pageSize, Guid? productId = null);
}

