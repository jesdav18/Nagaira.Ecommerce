using Nagaira.Ecommerce.Application.Pricing;
using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.Tests;

public class OfferPriceCalculatorTests
{
    [Fact]
    public void PercentageOffer_AppliesToQuantityPrice()
    {
        var offer = CreateOffer(priority: 1, percentage: 20);

        var result = OfferPriceCalculator.Calculate(offer, 80m, 10, 800m);

        Assert.NotNull(result);
        Assert.Equal(64m, result.FinalUnitPrice);
        Assert.Equal(160m, result.TotalDiscount);
    }

    [Fact]
    public void MinimumPurchase_PreventsDiscountBelowThreshold()
    {
        var offer = CreateOffer(priority: 1, percentage: 10);
        offer.MinPurchaseAmount = 500m;

        var result = OfferPriceCalculator.Calculate(offer, 100m, 2, 200m);

        Assert.Null(result);
    }

    [Fact]
    public void FixedDiscount_NeverProducesNegativePrice()
    {
        var offer = CreateOffer(priority: 1, fixedAmount: 150m);

        var result = OfferPriceCalculator.Calculate(offer, 100m, 2, 200m);

        Assert.NotNull(result);
        Assert.Equal(0m, result.FinalUnitPrice);
        Assert.Equal(200m, result.TotalDiscount);
    }

    [Fact]
    public void SelectOffer_AppliesOnlyHighestPriorityEligibleOffer()
    {
        var lowPriority = CreateOffer(priority: 1, percentage: 50);
        var highPriority = CreateOffer(priority: 10, percentage: 10);

        var result = OfferPriceCalculator.SelectOffer(
            new[] { lowPriority, highPriority },
            100m,
            1,
            100m);

        Assert.NotNull(result);
        Assert.Equal(highPriority.Id, result.Offer.Id);
        Assert.Equal(90m, result.FinalUnitPrice);
    }

    [Fact]
    public void SelectOffer_SkipsHigherPriorityOfferThatDoesNotMeetConditions()
    {
        var conditional = CreateOffer(priority: 10, percentage: 50);
        conditional.MinQuantity = 5;
        var fallback = CreateOffer(priority: 1, percentage: 10);

        var result = OfferPriceCalculator.SelectOffer(
            new[] { conditional, fallback },
            100m,
            1,
            100m);

        Assert.NotNull(result);
        Assert.Equal(fallback.Id, result.Offer.Id);
        Assert.Equal(90m, result.FinalUnitPrice);
    }

    [Fact]
    public void UnsupportedOfferType_IsNotApplied()
    {
        var offer = CreateOffer(priority: 1, percentage: 10);
        offer.OfferType = OfferType.BuyXGetY;

        Assert.Null(OfferPriceCalculator.Calculate(offer, 100m, 1, 100m));
    }

    [Fact]
    public void UnknownRule_FailsClosed()
    {
        var offer = CreateOffer(priority: 1, percentage: 10);
        offer.Rules.Add(new OfferRule { RuleType = "unknown_rule", Value = 1 });

        Assert.Null(OfferPriceCalculator.Calculate(offer, 100m, 1, 100m));
    }

    private static Offer CreateOffer(int priority, decimal? percentage = null, decimal? fixedAmount = null)
    {
        return new Offer
        {
            Id = Guid.NewGuid(),
            OfferType = fixedAmount.HasValue ? OfferType.FixedAmount : OfferType.Percentage,
            DiscountPercentage = percentage,
            DiscountAmount = fixedAmount,
            Priority = priority,
            Status = OfferStatus.Active,
            IsActive = true
        };
    }
}
