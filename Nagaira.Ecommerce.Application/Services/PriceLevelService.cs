using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class PriceLevelService : IPriceLevelService
{
    private readonly IUnitOfWork _unitOfWork;

    public PriceLevelService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<PriceLevelDto>> GetAllPriceLevelsAsync()
    {
        var levels = await _unitOfWork.PriceLevels.GetAllAsync();
        return levels.Select(MapToDto);
    }

    public async Task<PriceLevelDto?> GetPriceLevelByIdAsync(Guid id)
    {
        var level = await _unitOfWork.PriceLevels.GetByIdAsync(id);
        return level != null ? MapToDto(level) : null;
    }

    public async Task<PriceLevelDto> CreatePriceLevelAsync(CreatePriceLevelDto dto)
    {
        var level = new PriceLevel
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Priority = dto.Priority,
            MarkupPercentage = dto.MarkupPercentage,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PriceLevels.AddAsync(level);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(level);
    }

    public async Task UpdatePriceLevelAsync(UpdatePriceLevelDto dto)
    {
        var level = await _unitOfWork.PriceLevels.GetByIdAsync(dto.Id);
        if (level == null) throw new Exception("Price level not found");

        level.Name = dto.Name;
        level.Description = dto.Description;
        level.Priority = dto.Priority;
        level.MarkupPercentage = dto.MarkupPercentage;
        level.IsActive = dto.IsActive;
        level.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.PriceLevels.UpdateAsync(level);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeletePriceLevelAsync(Guid id)
    {
        await _unitOfWork.PriceLevels.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    private static PriceLevelDto MapToDto(PriceLevel level)
    {
        return new PriceLevelDto(
            level.Id,
            level.Name,
            level.Description,
            level.Priority,
            level.MarkupPercentage,
            level.IsActive
        );
    }
}

