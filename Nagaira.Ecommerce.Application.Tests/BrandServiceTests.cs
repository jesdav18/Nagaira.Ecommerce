using Moq;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Services;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Tests;

public class BrandServiceTests
{
    [Theory]
    [InlineData("NIVEA", "nivea")]
    [InlineData("Nívea", "nivea")]
    [InlineData(" nivea ", "nivea")]
    [InlineData("Oral B", "oralb")]
    [InlineData("ORAL-B", "oralb")]
    public void NormalizeName_CollapsesLogicalDuplicates(string input, string expected) =>
        Assert.Equal(expected, BrandService.NormalizeName(input));

    [Fact]
    public async Task Create_NewBrandPersistsIt()
    {
        var repo = new Mock<IBrandRepository>();
        Brand? added = null;
        repo.Setup(r => r.GetByNormalizedNameAsync("nivea")).ReturnsAsync((Brand?)null);
        repo.Setup(r => r.AddAsync(It.IsAny<Brand>())).Callback<Brand>(x => added = x).ReturnsAsync((Brand x) => x);
        var uow = new Mock<IUnitOfWork>(); uow.SetupGet(x => x.Brands).Returns(repo.Object); uow.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var result = await new BrandService(uow.Object).CreateAsync(new SaveBrandDto(" Nívea "));

        Assert.Equal("Nívea", result.Name); Assert.Equal("nivea", added!.NormalizedName); repo.Verify(r => r.AddAsync(It.IsAny<Brand>()), Times.Once);
    }

    [Fact]
    public async Task Create_EquivalentBrandReturnsExistingWithoutDuplicate()
    {
        var existing = new Brand { Id = Guid.NewGuid(), Name = "NIVEA", NormalizedName = "nivea", IsActive = true };
        var repo = new Mock<IBrandRepository>(); repo.Setup(r => r.GetByNormalizedNameAsync("nivea")).ReturnsAsync(existing);
        var uow = new Mock<IUnitOfWork>(); uow.SetupGet(x => x.Brands).Returns(repo.Object);

        var result = await new BrandService(uow.Object).CreateAsync(new SaveBrandDto("nívea"));

        Assert.Equal(existing.Id, result.Id); repo.Verify(r => r.AddAsync(It.IsAny<Brand>()), Times.Never);
    }
}
