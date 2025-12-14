using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class AppSettingService : IAppSettingService
{
    private readonly IUnitOfWork _unitOfWork;

    public AppSettingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AppSettingDto>> GetAllSettingsAsync()
    {
        var settings = await _unitOfWork.AppSettings.GetAllAsync();
        return settings.Select(MapToDto);
    }

    public async Task<AppSettingDto?> GetSettingByKeyAsync(string key)
    {
        var setting = await _unitOfWork.AppSettings.GetByKeyAsync(key);
        if (setting == null || setting.IsDeleted) return null;
        return MapToDto(setting);
    }

    public async Task<IEnumerable<AppSettingDto>> GetSettingsByCategoryAsync(string category)
    {
        var settings = await _unitOfWork.AppSettings.GetByCategoryAsync(category);
        return settings.Select(MapToDto);
    }

    public async Task<AppSettingDto> CreateSettingAsync(CreateAppSettingDto dto)
    {
        var existing = await _unitOfWork.AppSettings.GetByKeyAsync(dto.Key);
        if (existing != null && !existing.IsDeleted)
            throw new Exception($"Ya existe una configuración con la clave '{dto.Key}'");

        var setting = new AppSetting
        {
            Id = Guid.NewGuid(),
            Key = dto.Key,
            Value = dto.Value,
            Label = dto.Label,
            Description = dto.Description,
            Category = dto.Category,
            DataType = dto.DataType,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AppSettings.AddAsync(setting);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(setting);
    }

    public async Task UpdateSettingAsync(UpdateAppSettingDto dto)
    {
        var setting = await _unitOfWork.AppSettings.GetByIdAsync(dto.Id);
        if (setting == null || setting.IsDeleted)
            throw new Exception("Configuración no encontrada");

        var existingWithKey = await _unitOfWork.AppSettings.GetByKeyAsync(dto.Key);
        if (existingWithKey != null && existingWithKey.Id != dto.Id && !existingWithKey.IsDeleted)
            throw new Exception($"Ya existe otra configuración con la clave '{dto.Key}'");

        setting.Key = dto.Key;
        setting.Value = dto.Value;
        setting.Label = dto.Label;
        setting.Description = dto.Description;
        setting.Category = dto.Category;
        setting.DataType = dto.DataType;
        setting.DisplayOrder = dto.DisplayOrder;
        setting.IsActive = dto.IsActive;
        setting.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.AppSettings.UpdateAsync(setting);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteSettingAsync(Guid id)
    {
        await _unitOfWork.AppSettings.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<string?> GetSettingValueAsync(string key)
    {
        var setting = await _unitOfWork.AppSettings.GetByKeyAsync(key);
        if (setting == null || setting.IsDeleted || !setting.IsActive)
            return null;
        return setting.Value;
    }

    public async Task<T?> GetSettingValueAsync<T>(string key) where T : struct
    {
        var value = await GetSettingValueAsync(key);
        if (string.IsNullOrEmpty(value))
            return null;

        try
        {
            if (typeof(T) == typeof(decimal))
                return (T)(object)decimal.Parse(value);
            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(value);
            if (typeof(T) == typeof(double))
                return (T)(object)double.Parse(value);
            if (typeof(T) == typeof(bool))
                return (T)(object)bool.Parse(value);
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static AppSettingDto MapToDto(AppSetting setting)
    {
        return new AppSettingDto(
            setting.Id,
            setting.Key,
            setting.Value,
            setting.Label,
            setting.Description,
            setting.Category,
            setting.DataType,
            setting.DisplayOrder,
            setting.IsActive,
            setting.CreatedAt
        );
    }
}

