using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class ProductPriceService : IProductPriceService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductPriceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProductPriceDto>> GetPricesByProductAsync(Guid productId)
    {
        var prices = await _unitOfWork.ProductPrices.GetByProductIdAsync(productId);
        return prices.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductPriceDto>> GetPricesByLevelAsync(Guid priceLevelId)
    {
        var prices = await _unitOfWork.ProductPrices.GetByPriceLevelIdAsync(priceLevelId);
        return prices.Select(MapToDto);
    }

    public async Task<ProductPriceDto?> GetPriceByIdAsync(Guid id)
    {
        var price = await _unitOfWork.ProductPrices.GetByIdAsync(id);
        return price != null ? MapToDto(price) : null;
    }

    public async Task<ProductPriceDto> CreatePriceAsync(CreateProductPriceDto dto)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
        if (product == null) throw new Exception("Product not found");

        var priceLevel = await _unitOfWork.PriceLevels.GetByIdAsync(dto.PriceLevelId);
        if (priceLevel == null) throw new Exception("Price level not found");

        var existingPrice = await _unitOfWork.ProductPrices.GetByProductAndLevelAsync(dto.ProductId, dto.PriceLevelId);
        if (existingPrice != null && !existingPrice.IsDeleted)
            throw new Exception("Price for this product and level already exists");

        var productPrice = new ProductPrice
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            PriceLevelId = dto.PriceLevelId,
            Price = dto.Price,
            MinQuantity = dto.MinQuantity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ProductPrices.AddAsync(productPrice);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(productPrice);
    }

    public async Task UpdatePriceAsync(UpdateProductPriceDto dto)
    {
        var productPrice = await _unitOfWork.ProductPrices.GetByIdAsync(dto.Id);
        if (productPrice == null) throw new Exception("Product price not found");

        productPrice.Price = dto.Price;
        productPrice.MinQuantity = dto.MinQuantity;
        productPrice.IsActive = dto.IsActive;
        productPrice.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ProductPrices.UpdateAsync(productPrice);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeletePriceAsync(Guid id)
    {
        await _unitOfWork.ProductPrices.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ActivatePriceAsync(Guid id)
    {
        var productPrice = await _unitOfWork.ProductPrices.GetByIdAsync(id);
        if (productPrice == null) throw new Exception("Product price not found");

        productPrice.IsActive = true;
        productPrice.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ProductPrices.UpdateAsync(productPrice);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivatePriceAsync(Guid id)
    {
        var productPrice = await _unitOfWork.ProductPrices.GetByIdAsync(id);
        if (productPrice == null) throw new Exception("Product price not found");

        productPrice.IsActive = false;
        productPrice.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ProductPrices.UpdateAsync(productPrice);
        await _unitOfWork.SaveChangesAsync();
    }

    private static ProductPriceDto MapToDto(ProductPrice price)
    {
        return new ProductPriceDto(
            price.Id,
            price.ProductId,
            price.PriceLevelId,
            price.PriceLevel?.Name ?? string.Empty,
            price.Price,
            price.MinQuantity,
            price.IsActive
        );
    }
}

