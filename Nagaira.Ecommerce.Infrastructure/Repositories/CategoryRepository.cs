using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Category>> GetActiveCategoriesWithSubCategoriesAsync()
    {
        var allCategories = await _dbSet
            .Where(c => c.IsActive && !c.IsDeleted)
            .ToListAsync();
        
        var categoryDict = allCategories.ToDictionary(c => c.Id);
        var childrenIds = new HashSet<Guid>();
        
        foreach (var category in allCategories)
        {
            category.SubCategories = new List<Category>();
        }
        
        foreach (var category in allCategories)
        {
            if (category.ParentCategoryId.HasValue)
            {
                childrenIds.Add(category.Id);
                
                if (categoryDict.TryGetValue(category.ParentCategoryId.Value, out var parent))
                {
                    parent.SubCategories.Add(category);
                }
            }
        }
        
        return allCategories.Where(c => !childrenIds.Contains(c.Id)).ToList();
    }

    public async Task<List<Guid>> GetAllCategoryIdsRecursiveAsync(Guid categoryId)
    {
        var allCategories = await _dbSet
            .Where(c => c.IsActive && !c.IsDeleted)
            .ToListAsync();
        
        var categoryDict = allCategories.ToDictionary(c => c.Id);
        var result = new List<Guid> { categoryId };
        
        void CollectChildren(Guid parentId)
        {
            var children = allCategories.Where(c => c.ParentCategoryId == parentId).ToList();
            foreach (var child in children)
            {
                result.Add(child.Id);
                CollectChildren(child.Id);
            }
        }
        
        CollectChildren(categoryId);
        return result;
    }
}

