using System.Globalization;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.MetaCatalog;

public static class MetaCatalogProductMapper
{
    public static MetaCatalogMappingResult Map(Product product, MetaCatalogOptions options, IMetaCatalogPayloadHasher? hasher = null)
    {
        hasher ??= new MetaCatalogPayloadHasher();
        var retailerId = product.Id.ToString("D");

        if (product.IsDeleted || !product.IsActive)
        {
            return new MetaCatalogMappingResult(
                MetaCatalogSyncAction.Delete,
                retailerId,
                null,
                hasher.HashDelete(retailerId)
            );
        }

        var item = MapProduct(product, options, retailerId);
        if (item == null)
        {
            return new MetaCatalogMappingResult(
                MetaCatalogSyncAction.Delete,
                retailerId,
                null,
                hasher.HashDelete(retailerId)
            );
        }

        return new MetaCatalogMappingResult(
            MetaCatalogSyncAction.Upsert,
            retailerId,
            item,
            hasher.HashUpsert(item)
        );
    }

    private static MetaCatalogProduct? MapProduct(Product product, MetaCatalogOptions options, string retailerId)
    {

        var brand = product.Brand?.Trim();
        if (string.IsNullOrWhiteSpace(brand))
        {
            return null;
        }

        var image = product.Images
            .Where(i => !i.IsDeleted && !string.IsNullOrWhiteSpace(i.ImageUrl))
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.DisplayOrder)
            .FirstOrDefault();
        if (image == null)
        {
            return null;
        }

        var price = ResolvePublicPrice(product, options.PublicPriceLevelId);
        if (price == null)
        {
            return null;
        }

        var url = BuildProductUrl(options.PublicBaseUrl, product.Slug);
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var availability = product.HasVirtualStock || (product.InventoryBalance?.AvailableQuantity ?? 0) > 0
            ? "in stock"
            : "out of stock";

        return new MetaCatalogProduct(
            retailerId,
            product.Name.Trim(),
            product.Description?.Trim() ?? string.Empty,
            brand,
            availability,
            "new",
            price.Value.ToString("0.00", CultureInfo.InvariantCulture),
            options.Currency,
            url,
            image.ImageUrl,
            product.Category?.Name,
            product.Sku
        );
    }

    private static decimal? ResolvePublicPrice(Product product, Guid? publicPriceLevelId)
    {
        var activePrices = product.Prices
            .Where(p => p.IsActive && !p.IsDeleted && p.MinQuantity <= 1)
            .ToList();

        if (publicPriceLevelId.HasValue)
        {
            return activePrices.FirstOrDefault(p => p.PriceLevelId == publicPriceLevelId.Value)?.Price;
        }

        return activePrices
            .OrderBy(p => p.PriceLevel?.Priority ?? int.MaxValue)
            .ThenBy(p => p.MinQuantity)
            .FirstOrDefault()
            ?.Price;
    }

    private static string BuildProductUrl(string publicBaseUrl, string slug)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl) || string.IsNullOrWhiteSpace(slug))
        {
            return string.Empty;
        }

        return $"{publicBaseUrl.TrimEnd('/')}/p/{slug.TrimStart('/')}";
    }
}
