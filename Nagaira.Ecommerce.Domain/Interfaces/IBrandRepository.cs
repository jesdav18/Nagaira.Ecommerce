using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IBrandRepository : IRepository<Brand>
{
    Task<IReadOnlyList<Brand>> SearchAsync(string? search, bool activeOnly);
    Task<Brand?> GetByNormalizedNameAsync(string normalizedName);
}
