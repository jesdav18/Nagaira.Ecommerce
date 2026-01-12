using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> GetActiveCategoriesWithSubCategoriesAsync();
    Task<List<Guid>> GetAllCategoryIdsRecursiveAsync(Guid categoryId);
    Task<Category?> GetBySlugAsync(string slug);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
}

