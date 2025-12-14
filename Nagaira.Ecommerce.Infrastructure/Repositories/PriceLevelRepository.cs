using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class PriceLevelRepository : Repository<PriceLevel>, IPriceLevelRepository
{
    public PriceLevelRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<PriceLevel>> GetAllAsync()
    {
        return await _dbSet
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Priority)
            .ToListAsync();
    }

    public async Task<PriceLevel?> GetByNameAsync(string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Name == name && !p.IsDeleted);
    }

    public async Task<IEnumerable<PriceLevel>> GetActiveLevelsAsync()
    {
        return await _dbSet
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Priority)
            .ToListAsync();
    }
}

