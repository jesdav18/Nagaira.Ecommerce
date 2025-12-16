using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _unitOfWork.Products.GetActiveProductsAsync();
        return products.Select(MapToDto);
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null || !product.IsActive) return null;
        return MapToDto(product);
    }

    public async Task<ProductDto?> GetProductByIdWithPriceLevelAsync(Guid id, Guid? priceLevelId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        return product != null ? MapToDto(product) : null;
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(Guid categoryId)
    {
        var products = await _unitOfWork.Products.GetByCategoryAsync(categoryId);
        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        var products = await _unitOfWork.Products.SearchAsync(searchTerm);
        return products.Select(MapToDto);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        // Verificar que la categoría existe
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(dto.CategoryId);
        if (category == null || category.IsDeleted)
            throw new Exception("Category not found or is deleted");

        // Verificar que el SKU no existe (sin filtros de IsDeleted)
        var skuExists = await _unitOfWork.Products.SkuExistsAsync(dto.Sku);
        if (skuExists)
            throw new Exception($"El SKU '{dto.Sku}' ya existe. Por favor, use un SKU diferente.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Sku = dto.Sku,
            CategoryId = dto.CategoryId,
            Cost = dto.Cost,
            HasVirtualStock = dto.HasVirtualStock,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            
            if (dto.Prices != null && dto.Prices.Any())
            {
                foreach (var priceDto in dto.Prices)
                {
                    var priceLevel = await _unitOfWork.PriceLevels.GetByIdAsync(priceDto.PriceLevelId);
                    if (priceLevel == null) continue;
                    
                    var productPrice = new ProductPrice
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        PriceLevelId = priceDto.PriceLevelId,
                        Price = priceDto.Price,
                        MinQuantity = priceDto.MinQuantity,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.ProductPrices.AddAsync(productPrice);
                }
            }
            
            if (dto.Images != null && dto.Images.Any())
            {
                foreach (var imageDto in dto.Images)
                {
                    var productImage = new ProductImage
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        ImageUrl = imageDto.ImageUrl,
                        AltText = imageDto.AltText,
                        IsPrimary = imageDto.IsPrimary,
                        DisplayOrder = imageDto.DisplayOrder,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<ProductImage>().AddAsync(productImage);
                }
            }
            
            await _unitOfWork.SaveChangesAsync();
            
            var createdProduct = await _unitOfWork.Products.GetByIdAsync(product.Id);
            return MapToDto(createdProduct!);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al crear el producto: {ex.Message}. Inner: {ex.InnerException?.Message}", ex);
        }
    }

    public async Task UpdateProductAsync(UpdateProductDto dto)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(dto.Id);
        if (product == null) throw new Exception("Product not found");

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Cost = dto.Cost;
        product.IsActive = dto.IsActive;
        product.HasVirtualStock = dto.HasVirtualStock;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteProductAsync(Guid id)
    {
        await _unitOfWork.Products.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsForAdminAsync()
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        return products.Select(MapToDto);
    }

    public async Task<ProductDto?> GetProductByIdForAdminAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        return product != null ? MapToDto(product) : null;
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Sku,
            product.IsActive,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.InventoryBalance?.AvailableQuantity ?? 0,
            product.InventoryBalance?.ReservedQuantity ?? 0,
            product.Cost,
            product.HasVirtualStock,
            product.Images.Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.AltText, i.IsPrimary, i.DisplayOrder)).ToList(),
            product.Prices.Select(p => new ProductPriceDto(
                p.Id,
                p.ProductId,
                p.PriceLevelId,
                p.PriceLevel?.Name ?? string.Empty,
                p.Price,
                p.MinQuantity,
                p.IsActive
            )).ToList()
        );
    }
}
