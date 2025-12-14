using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IAppSettingService
{
    Task<IEnumerable<AppSettingDto>> GetAllSettingsAsync();
    Task<AppSettingDto?> GetSettingByKeyAsync(string key);
    Task<IEnumerable<AppSettingDto>> GetSettingsByCategoryAsync(string category);
    Task<AppSettingDto> CreateSettingAsync(CreateAppSettingDto dto);
    Task UpdateSettingAsync(UpdateAppSettingDto dto);
    Task DeleteSettingAsync(Guid id);
    Task<string?> GetSettingValueAsync(string key);
    Task<T?> GetSettingValueAsync<T>(string key) where T : struct;
}

