using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.Pricing;

public static class OfferPriceCalculator
{
    public static OfferPriceResult? SelectOffer(
        IEnumerable<Offer> offers,
        decimal baseUnitPrice,
        int quantity,
        decimal cartBaseTotal)
    {
        if (baseUnitPrice <= 0 || quantity <= 0)
            return null;

        foreach (var offer in offers
                     .Where(o => o.IsActive && o.Status == OfferStatus.Active)
                     .OrderByDescending(o => o.Priority)
                     .ThenBy(o => o.Id.ToString("D"), StringComparer.Ordinal))
        {
            var result = Calculate(offer, baseUnitPrice, quantity, cartBaseTotal);
            if (result != null)
                return result;
        }

        return null;
    }

    public static OfferPriceResult? Calculate(
        Offer offer,
        decimal baseUnitPrice,
        int quantity,
        decimal cartBaseTotal)
    {
        if (baseUnitPrice <= 0 || quantity <= 0)
            return null;
        if (offer.MinQuantity.HasValue && quantity < offer.MinQuantity.Value)
            return null;
        if (offer.MinPurchaseAmount.HasValue && cartBaseTotal < offer.MinPurchaseAmount.Value)
            return null;
        if (!RulesSatisfied(offer, baseUnitPrice, quantity, cartBaseTotal))
            return null;

        var requestedDiscount = offer.OfferType switch
        {
            OfferType.Percentage when offer.DiscountPercentage is > 0 and <= 100
                => baseUnitPrice * (offer.DiscountPercentage.Value / 100m),
            OfferType.FixedAmount when offer.DiscountAmount is > 0
                => offer.DiscountAmount.Value,
            _ => 0m
        };

        var unitDiscount = Math.Min(requestedDiscount, baseUnitPrice);
        if (unitDiscount <= 0)
            return null;

        return new OfferPriceResult(
            offer,
            baseUnitPrice - unitDiscount,
            unitDiscount,
            unitDiscount * quantity);
    }

    private static bool RulesSatisfied(Offer offer, decimal itemUnitPrice, int quantity, decimal cartTotal)
    {
        var itemSubtotal = itemUnitPrice * quantity;
        foreach (var rule in offer.Rules.Where(r => !r.IsDeleted))
        {
            var satisfied = rule.RuleType?.Trim().ToLowerInvariant() switch
            {
                "min_item_price" => itemUnitPrice >= rule.Value,
                "max_item_price" => itemUnitPrice <= rule.Value,
                "min_item_subtotal" => itemSubtotal >= rule.Value,
                "max_item_subtotal" => itemSubtotal <= rule.Value,
                "min_cart_total" => cartTotal >= rule.Value,
                _ => false
            };

            if (!satisfied)
                return false;
        }

        return true;
    }
}

public sealed record OfferPriceResult(
    Offer Offer,
    decimal FinalUnitPrice,
    decimal UnitDiscount,
    decimal TotalDiscount);
