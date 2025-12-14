using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class AppSettingRepository : Repository<AppSetting>, IAppSettingRepository
{
    public AppSettingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<AppSetting>> GetAllAsync()
    {
        return await _dbSet
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<AppSetting?> GetByKeyAsync(string key)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted);
    }

    public async Task<IEnumerable<AppSetting>> GetByCategoryAsync(string category)
    {
        return await _dbSet
            .Where(s => s.Category == category && !s.IsDeleted && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<AppSetting>> GetActiveSettingsAsync()
    {
        return await _dbSet
            .Where(s => !s.IsDeleted && s.IsActive)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();
    }
}

