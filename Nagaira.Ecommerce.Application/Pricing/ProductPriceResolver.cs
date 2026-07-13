using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.Pricing;

public static class ProductPriceResolver
{
    public static decimal? ResolveUnitPrice(IEnumerable<ProductPrice> prices, int quantity)
    {
        return ResolveUnitPrice(prices, quantity, useQuantityBreaks: true);
    }

    public static decimal? ResolveUnitPrice(IEnumerable<ProductPrice> prices, int quantity, bool useQuantityBreaks)
    {
        var activePrices = prices
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.MinQuantity)
            .ToList();

        if (activePrices.Count == 0)
        {
            return null;
        }

        if (!useQuantityBreaks)
        {
            return activePrices.First().Price;
        }

        var matchedPrice = activePrices
            .OrderByDescending(p => p.MinQuantity)
            .FirstOrDefault(p => quantity >= p.MinQuantity);
        return matchedPrice?.Price ?? activePrices.First().Price;
    }
}
