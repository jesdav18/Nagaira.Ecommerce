using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync(Guid? userId = null);
    Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync(Guid? userId = null);
    Task<ProductDto?> GetProductByIdAsync(Guid id, Guid? userId = null);
    Task<ProductDto?> GetProductBySlugAsync(string slug, Guid? userId = null);
    Task<ProductDto?> GetProductByIdWithPriceLevelAsync(Guid id, Guid? priceLevelId);
    Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(Guid categoryId, Guid? userId = null);
    Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm, Guid? userId = null);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto);
    Task UpdateProductAsync(UpdateProductDto dto);
    Task DeleteProductAsync(Guid id);
    Task<IEnumerable<ProductDto>> GetAllProductsForAdminAsync();
    Task<ProductDto?> GetProductByIdForAdminAsync(Guid id);
}
