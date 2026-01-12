namespace Nagaira.Ecommerce.Application.DTOs;

public record CreateBannerDto(
    string Title,
    string? Subtitle,
    string ImageUrl,
    string? LinkUrl,
    int DisplayOrder,
    bool IsActive
);

public record UpdateBannerDto(
    string? Title,
    string? Subtitle,
    string? ImageUrl,
    string? LinkUrl,
    int? DisplayOrder,
    bool? IsActive
);
