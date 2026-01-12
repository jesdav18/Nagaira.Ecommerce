namespace Nagaira.Ecommerce.Application.DTOs;

public record BannerDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string ImageUrl,
    string? LinkUrl,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAt
);
