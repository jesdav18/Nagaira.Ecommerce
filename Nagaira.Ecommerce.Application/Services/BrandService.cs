using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public partial class BrandService(IUnitOfWork unitOfWork) : IBrandService
{
    public async Task<IReadOnlyList<BrandDto>> SearchAsync(string? search, bool activeOnly) =>
        (await unitOfWork.Brands.SearchAsync(search, activeOnly)).Select(Map).ToList();

    public async Task<BrandDto> CreateAsync(SaveBrandDto dto)
    {
        var name = CleanName(dto.Name);
        var normalized = NormalizeName(name);
        var existing = await unitOfWork.Brands.GetByNormalizedNameAsync(normalized);
        if (existing != null) return Map(existing);
        var brand = new Brand { Id = Guid.NewGuid(), Name = name, NormalizedName = normalized, IsActive = dto.IsActive, CreatedAt = DateTime.UtcNow };
        await unitOfWork.Brands.AddAsync(brand);
        await unitOfWork.SaveChangesAsync();
        return Map(brand);
    }

    public async Task<BrandDto> UpdateAsync(Guid id, SaveBrandDto dto)
    {
        var brand = await unitOfWork.Brands.GetByIdAsync(id) ?? throw new KeyNotFoundException("Brand not found");
        var name = CleanName(dto.Name);
        var normalized = NormalizeName(name);
        var duplicate = await unitOfWork.Brands.GetByNormalizedNameAsync(normalized);
        if (duplicate != null && duplicate.Id != id) throw new InvalidOperationException("brand_already_exists");
        brand.Name = name; brand.NormalizedName = normalized; brand.IsActive = dto.IsActive; brand.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.Brands.UpdateAsync(brand);
        await unitOfWork.SaveChangesAsync();
        return Map(brand);
    }

    public static string NormalizeName(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return SeparatorRegex().Replace(withoutMarks.Normalize(NormalizationForm.FormC), string.Empty);
    }
    private static string CleanName(string value)
    {
        var cleaned = SpaceRegex().Replace(value.Trim(), " ");
        return string.IsNullOrWhiteSpace(cleaned) ? throw new ArgumentException("Brand name is required") : cleaned;
    }
    private static BrandDto Map(Brand b) => new(b.Id, b.Name, b.IsActive, b.CreatedAt, b.UpdatedAt);
    [GeneratedRegex(@"[\s\p{P}\p{S}]+", RegexOptions.CultureInvariant)] private static partial Regex SeparatorRegex();
    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)] private static partial Regex SpaceRegex();
}
