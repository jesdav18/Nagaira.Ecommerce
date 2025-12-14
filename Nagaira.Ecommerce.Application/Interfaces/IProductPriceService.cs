using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IProductPriceService
{
    Task<IEnumerable<ProductPriceDto>> GetPricesByProductAsync(Guid productId);
    Task<IEnumerable<ProductPriceDto>> GetPricesByLevelAsync(Guid priceLevelId);
    Task<ProductPriceDto?> GetPriceByIdAsync(Guid id);
    Task<ProductPriceDto> CreatePriceAsync(CreateProductPriceDto dto);
    Task UpdatePriceAsync(UpdateProductPriceDto dto);
    Task DeletePriceAsync(Guid id);
    Task ActivatePriceAsync(Guid id);
    Task DeactivatePriceAsync(Guid id);
}

