using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IAppSettingRepository : IRepository<AppSetting>
{
    Task<AppSetting?> GetByKeyAsync(string key);
    Task<IEnumerable<AppSetting>> GetByCategoryAsync(string category);
    Task<IEnumerable<AppSetting>> GetActiveSettingsAsync();
}

