using System.ComponentModel.DataAnnotations;

namespace Nagaira.Ecommerce.Application.DTOs;

public record BrandDto(Guid Id, string Name, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
public record SaveBrandDto([Required, StringLength(255)] string Name, bool IsActive = true);
