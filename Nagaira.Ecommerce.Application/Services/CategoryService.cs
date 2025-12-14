using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

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

    public async Task<CategoryDto?> GetActiveCategoryByIdAsync(Guid id)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
        if (category == null || !category.IsActive || category.IsDeleted) return null;
        return MapToDto(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var slug = dto.Name.ToLower().Replace(" ", "-");
        
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

        var slug = dto.Name.ToLower().Replace(" ", "-");
        
        category.Name = dto.Name;
        category.Description = dto.Description;
        category.Slug = slug;
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
}
