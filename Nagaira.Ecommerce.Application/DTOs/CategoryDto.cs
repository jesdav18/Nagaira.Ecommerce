namespace Nagaira.Ecommerce.Application.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    string Slug,
    string? ImageUrl,
    bool IsActive,
    Guid? ParentCategoryId,
    IEnumerable<CategoryDto>? SubCategories = null
);

public record CreateCategoryDto(
    string Name,
    string Description,
    string? ImageUrl,
    Guid? ParentCategoryId
);

public record UpdateCategoryDto(
    string Name,
    string Description,
    string? ImageUrl,
    Guid? ParentCategoryId,
    bool IsActive
);
