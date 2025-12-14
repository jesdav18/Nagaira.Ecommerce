namespace Nagaira.Ecommerce.Application.DTOs;

public record PriceLevelDto(
    Guid Id,
    string Name,
    string? Description,
    int Priority,
    decimal MarkupPercentage,
    bool IsActive
);

public record CreatePriceLevelDto(
    string Name,
    string? Description,
    int Priority,
    decimal MarkupPercentage
);

public record UpdatePriceLevelDto(
    Guid Id,
    string Name,
    string? Description,
    int Priority,
    decimal MarkupPercentage,
    bool IsActive
);

