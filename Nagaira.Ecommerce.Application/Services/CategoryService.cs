using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Nagaira.Ecommerce.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var rootCategories = await _unitOfWork.Categories.GetActiveCategoriesWithSubCategoriesAsync();
        return rootCategories.Select(MapToDto);
    }

    public async Task<IEnumerable<CategoryDto>> GetAllActiveCategoriesAsync()
    {
        var rootCategories = await _unitOfWork.Categories.GetActiveCategoriesWithSubCategoriesAsync();
        return rootCategories.Select(MapToDto);
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
        if (category == null || !category.IsActive) return null;
        return MapToDto(category);
    }

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug)
    {
        var category = await _unitOfWork.Categories.GetBySlugAsync(slug);
        return category != null ? MapToDto(category) : null;
    }

    public async Task<CategoryDto?> GetActiveCategoryByIdAsync(Guid id)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
        if (category == null || !category.IsActive || category.IsDeleted) return null;
        return MapToDto(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var slug = await GenerateUniqueSlugAsync(dto.Name);
        
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Slug = slug,
            ImageUrl = dto.ImageUrl,
            ParentCategoryId = dto.ParentCategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Category>().AddAsync(category);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(category);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
        if (category == null)
            throw new KeyNotFoundException($"Category with id {id} not found");

        if (!string.Equals(category.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
        {
            var newSlug = await GenerateUniqueSlugAsync(dto.Name, category.Id);
            if (!string.Equals(category.Slug, newSlug, StringComparison.OrdinalIgnoreCase))
            {
                await AddSlugHistoryAsync("category", category.Id, category.Slug);
                category.Slug = newSlug;
            }
        }
        
        category.Name = dto.Name;
        category.Description = dto.Description;
        category.ImageUrl = dto.ImageUrl;
        category.ParentCategoryId = dto.ParentCategoryId;
        category.IsActive = dto.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Category>().UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(category);
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        await _unitOfWork.Repository<Category>().DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesForAdminAsync()
    {
        var categories = await _unitOfWork.Repository<Category>().FindAsync(c => !c.IsDeleted);
        return categories.Select(MapToDto);
    }

    public async Task<CategoryDto?> GetCategoryByIdForAdminAsync(Guid id)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
        return category != null ? MapToDto(category) : null;
    }

    public async Task<int> SetFeaturedProductsForCategoryAsync(Guid categoryId, bool isFeatured)
    {
        var categories = await _unitOfWork.Repository<Category>().FindAsync(c => !c.IsDeleted);
        var categoryList = categories.ToList();
        if (!categoryList.Any(c => c.Id == categoryId))
        {
            throw new KeyNotFoundException($"Category with id {categoryId} not found");
        }

        var categoryIds = new HashSet<Guid> { categoryId };

        void CollectChildren(Guid parentId)
        {
            var children = categoryList.Where(c => c.ParentCategoryId == parentId).ToList();
            foreach (var child in children)
            {
                if (categoryIds.Add(child.Id))
                {
                    CollectChildren(child.Id);
                }
            }
        }

        CollectChildren(categoryId);

        var products = await _unitOfWork.Repository<Product>()
            .FindAsync(p => !p.IsDeleted && categoryIds.Contains(p.CategoryId));

        var updated = 0;
        foreach (var product in products)
        {
            if (product.IsFeatured != isFeatured)
            {
                product.IsFeatured = isFeatured;
                updated++;
            }
        }

        if (updated > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return updated;
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.Slug,
            category.ImageUrl,
            category.IsActive,
            category.ParentCategoryId,
            category.SubCategories?.Where(sc => sc.IsActive && !sc.IsDeleted).Select(MapToDto).ToList()
        );
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, Guid? excludeId = null)
    {
        var slugBase = Slugify(name);
        var slug = slugBase;
        var suffix = 2;

        while (await _unitOfWork.Categories.SlugExistsAsync(slug, excludeId))
        {
            slug = $"{slugBase}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        normalized = normalized.Normalize(NormalizationForm.FormD);
        var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
        normalized = new string(chars).Normalize(NormalizationForm.FormC);
        normalized = Regex.Replace(normalized, @"\s+", "-");
        normalized = Regex.Replace(normalized, @"[^a-z0-9\-]", "");
        normalized = Regex.Replace(normalized, @"-+", "-");
        return normalized.Trim('-');
    }

    private async Task AddSlugHistoryAsync(string entityType, Guid entityId, string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return;
        }

        var history = new SlugHistory
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Slug = slug,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Repository<SlugHistory>().AddAsync(history);
        await _unitOfWork.SaveChangesAsync();
    }
}
