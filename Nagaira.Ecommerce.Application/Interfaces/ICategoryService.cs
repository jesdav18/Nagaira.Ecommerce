using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<IEnumerable<CategoryDto>> GetAllActiveCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(Guid id);
    Task<CategoryDto?> GetActiveCategoryByIdAsync(Guid id);
    Task<IEnumerable<CategoryDto>> GetAllCategoriesForAdminAsync();
    Task<CategoryDto?> GetCategoryByIdForAdminAsync(Guid id);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto);
    Task DeleteCategoryAsync(Guid id);
}
