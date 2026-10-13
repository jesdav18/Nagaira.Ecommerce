using Moq;
using System.Linq.Expressions;
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
            DiscountAmount: 279m,
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

    [Fact]
    public async Task Update_FromPercentageToFixedAmount_PersistsTypeAndClearsPercentage()
    {
        var offer = new Offer
        {
            Id = Guid.NewGuid(),
            Name = "Oferta",
            OfferType = OfferType.Percentage,
            Status = OfferStatus.Active,
            DiscountPercentage = 22.38m,
            IsActive = true
        };
        var offerRepository = new Mock<IOfferRepository>();
        offerRepository.Setup(repository => repository.GetByIdAsync(offer.Id)).ReturnsAsync(offer);
        offerRepository.Setup(repository => repository.UpdateAsync(offer)).Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.Offers).Returns(offerRepository.Object);
        SetupEmptyRepository<OfferProduct>(unitOfWork);
        SetupEmptyRepository<OfferCategory>(unitOfWork);
        SetupEmptyRepository<OfferExcludedProduct>(unitOfWork);
        SetupEmptyRepository<OfferExcludedCategory>(unitOfWork);
        SetupEmptyRepository<OfferRule>(unitOfWork);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var now = DateTime.UtcNow;
        var dto = new UpdateOfferDto(
            offer.Id, offer.Name, null, "FixedAmount", "Active", 22.38m, 158m,
            null, null, null, null, now, now.AddDays(1), 0, true,
            null, null, null, null, []);

        await new OfferService(unitOfWork.Object).UpdateOfferAsync(dto);

        Assert.Equal(OfferType.FixedAmount, offer.OfferType);
        Assert.Equal(158m, offer.DiscountAmount);
        Assert.Null(offer.DiscountPercentage);
        Assert.Equal(158m, OfferPriceCalculator.SelectOffer([offer], 203.56m, 1, 203.56m)!.FinalUnitPrice);
    }

    private static void SetupEmptyRepository<T>(Mock<IUnitOfWork> unitOfWork) where T : class
    {
        var repository = new Mock<IRepository<T>>();
        repository
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<T, bool>>>()))
            .ReturnsAsync([]);
        unitOfWork.Setup(x => x.Repository<T>()).Returns(repository.Object);
    }
}
