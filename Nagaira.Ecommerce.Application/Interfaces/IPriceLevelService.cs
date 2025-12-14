using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IPriceLevelService
{
    Task<IEnumerable<PriceLevelDto>> GetAllPriceLevelsAsync();
    Task<PriceLevelDto?> GetPriceLevelByIdAsync(Guid id);
    Task<PriceLevelDto> CreatePriceLevelAsync(CreatePriceLevelDto dto);
    Task UpdatePriceLevelAsync(UpdatePriceLevelDto dto);
    Task DeletePriceLevelAsync(Guid id);
}

