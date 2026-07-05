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

        var matchedPrice = activePrices
            .OrderByDescending(p => p.MinQuantity)
            .FirstOrDefault(p => quantity >= p.MinQuantity);

        return matchedPrice?.Price ?? activePrices.First().Price;
    }
}
