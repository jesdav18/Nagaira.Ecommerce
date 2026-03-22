using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.Pricing;

public static class ProductPriceResolver
{
    public static decimal? ResolveUnitPrice(IEnumerable<ProductPrice> prices, int quantity)
    {
        var activePrices = prices
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.MinQuantity)
            .ToList();

        if (activePrices.Count == 0)
        {
            return null;
        }

        var retailPrice = activePrices.First().Price;
        if (quantity < 3)
        {
            return retailPrice;
        }

        var wholesalePrice = activePrices
            .FirstOrDefault(p => (p.PriceLevel?.Name ?? string.Empty)
                .Trim()
                .Contains("mayorista", StringComparison.OrdinalIgnoreCase));

        return wholesalePrice?.Price ?? retailPrice;
    }
}
