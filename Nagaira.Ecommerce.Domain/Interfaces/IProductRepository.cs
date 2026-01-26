using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId);
    Task<IEnumerable<Product>> GetActiveProductsAsync();
    Task<IEnumerable<Product>> GetFeaturedProductsAsync();
    Task<Product?> GetBySkuAsync(string sku);
    Task<Product?> GetBySkuIncludingDeletedAsync(string sku);
    Task<bool> SkuExistsAsync(string sku);
    Task<Product?> GetBySlugAsync(string slug);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm);
}
