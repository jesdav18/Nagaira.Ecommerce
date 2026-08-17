using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IBrandService
{
    Task<IReadOnlyList<BrandDto>> SearchAsync(string? search, bool activeOnly);
    Task<BrandDto> CreateAsync(SaveBrandDto dto);
    Task<BrandDto> UpdateAsync(Guid id, SaveBrandDto dto);
}
