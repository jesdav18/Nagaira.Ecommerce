using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class BrandRepository : Repository<Brand>, IBrandRepository
{
    public BrandRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Brand>> SearchAsync(string? search, bool activeOnly)
    {
        var query = _dbSet.AsNoTracking().Where(b => !b.IsDeleted);
        if (activeOnly) query = query.Where(b => b.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(b => EF.Functions.ILike(b.Name, pattern));
        }
        return await query.OrderBy(b => b.Name).Take(100).ToListAsync();
    }

    public Task<Brand?> GetByNormalizedNameAsync(string normalizedName) =>
        _dbSet.FirstOrDefaultAsync(b => b.NormalizedName == normalizedName && !b.IsDeleted);
}
