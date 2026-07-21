using System.Globalization;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.MetaCatalog;

public static class MetaCatalogProductMapper
{
    public static MetaCatalogMappingResult Map(Product product, MetaCatalogOptions options, IMetaCatalogPayloadHasher? hasher = null)
    {
        var outcome = TryMap(product, options, hasher);
        if (outcome.MappingResult == null)
        {
            throw new InvalidOperationException($"Product is not eligible for Meta Catalog sync: {outcome.Reason}");
        }

        return outcome.MappingResult;
    }

    public static MetaCatalogProductMappingOutcome TryMap(Product product, MetaCatalogOptions options, IMetaCatalogPayloadHasher? hasher = null)
    {
        hasher ??= new MetaCatalogPayloadHasher();
        var retailerId = product.Id.ToString("D");

        var item = MapProduct(product, options, retailerId, out var reason);
        if (item == null)
        {
            return new MetaCatalogProductMappingOutcome(
                MetaCatalogProductMappingStatus.Skipped,
                retailerId,
                null,
                reason);
        }

        if (product.IsDeleted || !product.IsActive)
        {
            return new MetaCatalogProductMappingOutcome(
                MetaCatalogProductMappingStatus.Delete,
                retailerId,
                new MetaCatalogMappingResult(
                    MetaCatalogSyncAction.Delete,
                    retailerId,
                    null,
                    hasher.HashDelete(retailerId)
                ),
                null
            );
        }

        return new MetaCatalogProductMappingOutcome(
            MetaCatalogProductMappingStatus.Upsert,
            retailerId,
            new MetaCatalogMappingResult(
                MetaCatalogSyncAction.Upsert,
                retailerId,
                item,
                hasher.HashUpsert(item)
            ),
            null
        );
    }

    private static MetaCatalogProduct? MapProduct(Product product, MetaCatalogOptions options, string retailerId, out string? reason)
    {
        reason = null;

        var brand = product.Brand?.Trim();
        if (string.IsNullOrWhiteSpace(brand))
        {
            reason = "missing_brand";
            return null;
        }

        var image = product.Images
            .Where(i => !i.IsDeleted && !string.IsNullOrWhiteSpace(i.ImageUrl))
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.DisplayOrder)
            .FirstOrDefault();
        if (image == null)
        {
            reason = "missing_image";
            return null;
        }

        var price = ResolvePublicPrice(product, options.PublicPriceLevelId);
        if (price == null)
        {
            reason = "missing_public_price";
            return null;
        }

        if (string.IsNullOrWhiteSpace(product.Slug))
        {
            reason = "missing_slug";
            return null;
        }

        if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            reason = "missing_public_base_url";
            return null;
        }

        var url = BuildProductUrl(options.PublicBaseUrl, product.Slug);
        if (string.IsNullOrWhiteSpace(url))
        {
            reason = string.IsNullOrWhiteSpace(options.PublicBaseUrl)
                ? "missing_public_base_url"
                : "missing_slug";
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

public enum MetaCatalogProductMappingStatus
{
    Upsert,
    Delete,
    Skipped
}

public record MetaCatalogProductMappingOutcome(
    MetaCatalogProductMappingStatus Status,
    string RetailerId,
    MetaCatalogMappingResult? MappingResult,
    string? Reason
);
