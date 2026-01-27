using System.Globalization;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using System.Text.RegularExpressions;
using System.Text;

namespace Nagaira.Ecommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(Guid? userId = null)
    {
        var products = await _unitOfWork.Products.GetActiveProductsAsync();
        var user = userId.HasValue ? await _unitOfWork.Users.GetByIdAsync(userId.Value) : null;
        return await MapToDtosAsync(products, user);
    }

    public async Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync(Guid? userId = null)
    {
        var products = await _unitOfWork.Products.GetFeaturedProductsAsync();
        var user = userId.HasValue ? await _unitOfWork.Users.GetByIdAsync(userId.Value) : null;
        return await MapToDtosAsync(products, user);
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, Guid? userId = null)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null || !product.IsActive) return null;
        var user = userId.HasValue ? await _unitOfWork.Users.GetByIdAsync(userId.Value) : null;
        return await MapToDtoAsync(product, user);
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug, Guid? userId = null)
    {
        var product = await _unitOfWork.Products.GetBySlugAsync(slug);
        if (product == null) return null;
        var user = userId.HasValue ? await _unitOfWork.Users.GetByIdAsync(userId.Value) : null;
        return await MapToDtoAsync(product, user);
    }

    public async Task<ProductDto?> GetProductByIdWithPriceLevelAsync(Guid id, Guid? priceLevelId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        return product != null ? MapToDto(product) : null;
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(Guid categoryId, Guid? userId = null)
    {
        var products = await _unitOfWork.Products.GetByCategoryAsync(categoryId);
        var user = userId.HasValue ? await _unitOfWork.Users.GetByIdAsync(userId.Value) : null;
        return await MapToDtosAsync(products, user);
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm, Guid? userId = null)
    {
        var products = await _unitOfWork.Products.SearchAsync(searchTerm);
        var user = userId.HasValue ? await _unitOfWork.Users.GetByIdAsync(userId.Value) : null;
        return await MapToDtosAsync(products, user);
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
            Slug = await GenerateUniqueSlugAsync(dto.Name),
            CategoryId = dto.CategoryId,
            Cost = dto.Cost,
            HasVirtualStock = dto.HasVirtualStock,
            IsFeatured = dto.IsFeatured,
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
                        PriceWithoutTax = priceDto.PriceWithoutTax,
                        MinQuantity = priceDto.MinQuantity,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.ProductPrices.AddAsync(productPrice);
                }
            }
            else
            {
                await CreateDefaultPricesAsync(product);
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

        if (!string.Equals(product.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
        {
            var newSlug = await GenerateUniqueSlugAsync(dto.Name, product.Id);
            if (!string.Equals(product.Slug, newSlug, StringComparison.OrdinalIgnoreCase))
            {
                await AddSlugHistoryAsync("product", product.Id, product.Slug);
                product.Slug = newSlug;
            }
        }

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Cost = dto.Cost;
        product.IsActive = dto.IsActive;
        product.HasVirtualStock = dto.HasVirtualStock;
        product.IsFeatured = dto.IsFeatured;
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
            product.Slug,
            product.IsActive,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.InventoryBalance?.AvailableQuantity ?? 0,
            product.InventoryBalance?.ReservedQuantity ?? 0,
            product.Cost,
            product.HasVirtualStock,
            product.IsFeatured,
            null,
            product.Images.Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.AltText, i.IsPrimary, i.DisplayOrder)).ToList(),
            product.Prices.Select(p => new ProductPriceDto(
                p.Id,
                p.ProductId,
                p.PriceLevelId,
                p.PriceLevel?.Name ?? string.Empty,
                p.Price,
                p.PriceWithoutTax,
                p.MinQuantity,
                p.IsActive
            )).ToList()
        );
    }

    private async Task<List<ProductDto>> MapToDtosAsync(IEnumerable<Product> products, User? user)
    {
        var result = new List<ProductDto>();
        foreach (var product in products)
        {
            result.Add(await MapToDtoAsync(product, user));
        }
        return result;
    }

    private async Task<ProductDto> MapToDtoAsync(Product product, User? user)
    {
        var offerPrice = user != null ? await GetOfferPriceAsync(product, user) : null;

        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Sku,
            product.Slug,
            product.IsActive,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.InventoryBalance?.AvailableQuantity ?? 0,
            product.InventoryBalance?.ReservedQuantity ?? 0,
            product.Cost,
            product.HasVirtualStock,
            product.IsFeatured,
            offerPrice,
            product.Images.Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.AltText, i.IsPrimary, i.DisplayOrder)).ToList(),
            product.Prices.Select(p => new ProductPriceDto(
                p.Id,
                p.ProductId,
                p.PriceLevelId,
                p.PriceLevel?.Name ?? string.Empty,
                p.Price,
                p.PriceWithoutTax,
                p.MinQuantity,
                p.IsActive
            )).ToList()
        );
    }

    private async Task<decimal?> GetOfferPriceAsync(Product product, User user)
    {
        var basePrice = GetBasePrice(product, user.PriceLevelId);
        if (basePrice <= 0)
        {
            return null;
        }

        var offers = await _unitOfWork.Offers.GetOffersForProductAsync(product.Id, DateTime.UtcNow);
        if (!offers.Any())
        {
            return null;
        }

        var quantity = 1;
        var cartTotal = basePrice;
        var finalPrice = basePrice;
        var hasDiscount = false;

        foreach (var offer in offers.OrderByDescending(o => o.Priority))
        {
            if (offer.TotalMaxUses.HasValue && offer.CurrentUses >= offer.TotalMaxUses.Value)
                continue;

            if (offer.MaxUsesPerCustomer.HasValue)
            {
                var userUsage = await _unitOfWork.Offers.GetUsageCountAsync(offer.Id, user.Id);
                if (userUsage >= offer.MaxUsesPerCustomer.Value)
                    continue;
            }

            if (offer.MinQuantity.HasValue && quantity < offer.MinQuantity.Value)
                continue;

            if (!OfferRulesSatisfied(offer, finalPrice, quantity, cartTotal))
                continue;

            decimal discount = 0;
            if (offer.OfferType == OfferType.Percentage && offer.DiscountPercentage.HasValue)
            {
                discount = finalPrice * (offer.DiscountPercentage.Value / 100);
            }
            else if (offer.OfferType == OfferType.FixedAmount && offer.DiscountAmount.HasValue)
            {
                discount = offer.DiscountAmount.Value;
            }

            if (discount <= 0)
                continue;

            finalPrice -= discount;
            if (finalPrice < 0)
                finalPrice = 0;

            hasDiscount = true;
        }

        return hasDiscount && finalPrice < basePrice ? finalPrice : null;
    }

    private static decimal GetBasePrice(Product product, Guid? priceLevelId)
    {
        var activePrices = product.Prices.Where(p => p.IsActive).ToList();
        if (activePrices.Count == 0)
        {
            return 0;
        }

        if (priceLevelId.HasValue)
        {
            var priceForLevel = activePrices.FirstOrDefault(p => p.PriceLevelId == priceLevelId.Value);
            if (priceForLevel != null)
            {
                return priceForLevel.Price;
            }
        }

        return activePrices.OrderBy(p => p.MinQuantity).First().Price;
    }

    private static bool OfferRulesSatisfied(Offer offer, decimal itemUnitPrice, int quantity, decimal cartTotal)
    {
        if (offer.Rules == null || offer.Rules.Count == 0) return true;

        var itemSubtotal = itemUnitPrice * quantity;
        foreach (var rule in offer.Rules.Where(r => !r.IsDeleted))
        {
            var type = rule.RuleType?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(type)) return false;

            switch (type)
            {
                case "min_item_price":
                    if (itemUnitPrice < rule.Value) return false;
                    break;
                case "max_item_price":
                    if (itemUnitPrice > rule.Value) return false;
                    break;
                case "min_item_subtotal":
                    if (itemSubtotal < rule.Value) return false;
                    break;
                case "max_item_subtotal":
                    if (itemSubtotal > rule.Value) return false;
                    break;
                case "min_cart_total":
                    if (cartTotal < rule.Value) return false;
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, Guid? excludeId = null)
    {
        var slugBase = Slugify(name);
        var slug = slugBase;
        var suffix = 2;

        while (await _unitOfWork.Products.SlugExistsAsync(slug, excludeId))
        {
            slug = $"{slugBase}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        normalized = normalized.Normalize(NormalizationForm.FormD);
        var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
        normalized = new string(chars).Normalize(NormalizationForm.FormC);
        normalized = Regex.Replace(normalized, @"\s+", "-");
        normalized = Regex.Replace(normalized, @"[^a-z0-9\-]", "");
        normalized = Regex.Replace(normalized, @"-+", "-");
        return normalized.Trim('-');
    }

    private async Task AddSlugHistoryAsync(string entityType, Guid entityId, string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return;
        }

        var history = new SlugHistory
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Slug = slug,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Repository<SlugHistory>().AddAsync(history);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task CreateDefaultPricesAsync(Product product)
    {
        if (!product.Cost.HasValue || product.Cost.Value <= 0)
        {
            return;
        }

        var levels = await _unitOfWork.PriceLevels.GetActiveLevelsAsync();
        var levelList = levels.ToList();
        if (levelList.Count == 0)
        {
            return;
        }

        var taxRate = await GetTaxRateAsync();
        var taxMultiplier = 1 + taxRate;

        foreach (var level in levelList)
        {
            var markupMultiplier = 1 + (level.MarkupPercentage / 100m);
            var priceWithTax = Math.Round(product.Cost.Value * markupMultiplier, 2);
            var priceWithoutTax = taxMultiplier > 0
                ? Math.Round(priceWithTax / taxMultiplier, 2)
                : priceWithTax;

            var productPrice = new ProductPrice
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                PriceLevelId = level.Id,
                Price = priceWithTax,
                PriceWithoutTax = priceWithoutTax,
                MinQuantity = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ProductPrices.AddAsync(productPrice);
        }
    }

    private async Task<decimal> GetTaxRateAsync()
    {
        var setting = await _unitOfWork.AppSettings.GetByKeyAsync("tax_rate");
        if (setting?.Value == null)
        {
            return 0.16m;
        }

        return decimal.TryParse(setting.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)
            ? rate
            : 0.16m;
    }
}
