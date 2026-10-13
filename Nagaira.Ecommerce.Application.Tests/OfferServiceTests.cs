using Moq;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Pricing;
using Nagaira.Ecommerce.Application.Services;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Tests;

public class OfferServiceTests
{
    [Fact]
    public async Task Create_ActiveFixedAmountOffer_PersistsAsActiveAndCanBeApplied()
    {
        Offer? savedOffer = null;
        var offerRepository = new Mock<IOfferRepository>();
        offerRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Offer>()))
            .Callback<Offer>(offer => savedOffer = offer)
            .ReturnsAsync((Offer offer) => offer);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.Offers).Returns(offerRepository.Object);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var now = DateTime.UtcNow;
        var dto = new CreateOfferDto(
            Name: "Descuento fijo",
            Description: null,
            OfferType: "FixedAmount",
            DiscountPercentage: null,
            DiscountAmount: 23.50m,
            MinPurchaseAmount: null,
            MinQuantity: null,
            MaxUsesPerCustomer: null,
            TotalMaxUses: null,
            StartDate: now.AddHours(-1),
            EndDate: now.AddDays(1),
            Priority: 0,
            ProductIds: null,
            CategoryIds: null,
            ExcludedProductIds: null,
            ExcludedCategoryIds: null,
            Rules: null,
            Status: "Active",
            IsActive: true);

        var result = await new OfferService(unitOfWork.Object).CreateOfferAsync(dto, Guid.NewGuid());
        var price = OfferPriceCalculator.SelectOffer([savedOffer!], 302.50m, 1, 302.50m);

        Assert.Equal("Active", result.Status);
        Assert.True(result.IsActive);
        Assert.Equal(279m, price!.FinalUnitPrice);
    }

    [Fact]
    public async Task Create_DraftOffer_DefaultsToInactive()
    {
        Offer? savedOffer = null;
        var offerRepository = new Mock<IOfferRepository>();
        offerRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Offer>()))
            .Callback<Offer>(offer => savedOffer = offer)
            .ReturnsAsync((Offer offer) => offer);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.Offers).Returns(offerRepository.Object);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var now = DateTime.UtcNow;
        var dto = new CreateOfferDto(
            "Borrador", null, "FixedAmount", null, 10m, null, null, null, null,
            now, now.AddDays(1), 0, null, null, null, null, null);

        var result = await new OfferService(unitOfWork.Object).CreateOfferAsync(dto, Guid.NewGuid());

        Assert.Equal("Draft", result.Status);
        Assert.False(result.IsActive);
        Assert.Null(OfferPriceCalculator.SelectOffer([savedOffer!], 100m, 1, 100m));
    }
}
