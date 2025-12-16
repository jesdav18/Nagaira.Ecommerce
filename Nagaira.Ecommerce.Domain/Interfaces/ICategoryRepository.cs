using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> GetActiveCategoriesWithSubCategoriesAsync();
    Task<List<Guid>> GetAllCategoryIdsRecursiveAsync(Guid categoryId);
}

