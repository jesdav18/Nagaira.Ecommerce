using Moq;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Services;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Tests;

public class ProductServiceBrandTests
{
    private static readonly Guid CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PriceLevelId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task CreateProduct_WithBrand_TrimsAndReturnsBrand()
    {
        Product? capturedProduct = null;
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.Products
            .Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(p => capturedProduct = p)
            .ReturnsAsync((Product p) => p);
        unitOfWork.Products
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => CreateProduct(capturedProduct!.Id, capturedProduct.Brand));
        var service = new ProductService(unitOfWork.UnitOfWork.Object);

        var result = await service.CreateProductAsync(CreateCreateDto(" Rexona "));

        Assert.Equal("Rexona", capturedProduct!.Brand);
        Assert.Equal("Rexona", result.Brand);
    }

    [Fact]
    public async Task UpdateProduct_UpdatesBrand()
    {
        var product = CreateProduct(brand: "Old Brand");
        var unitOfWork = CreateUnitOfWork(product);
        var service = new ProductService(unitOfWork.UnitOfWork.Object);

        await service.UpdateProductAsync(CreateUpdateDto(product, " Rexona "));

        Assert.Equal("Rexona", product.Brand);
        unitOfWork.Products.Verify(r => r.UpdateAsync(product), Times.Once);
    }

    [Fact]
    public async Task UpdateProduct_EmptyBrandStoresNull()
    {
        var product = CreateProduct(brand: "Old Brand");
        var unitOfWork = CreateUnitOfWork(product);
        var service = new ProductService(unitOfWork.UnitOfWork.Object);

        await service.UpdateProductAsync(CreateUpdateDto(product, "   "));

        Assert.Null(product.Brand);
    }

    [Fact]
    public async Task UpdateProduct_BrandLongerThanTwoHundredFiftyFiveFails()
    {
        var product = CreateProduct(brand: "Old Brand");
        var unitOfWork = CreateUnitOfWork(product);
        var service = new ProductService(unitOfWork.UnitOfWork.Object);
        var brand = new string('x', 256);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateProductAsync(CreateUpdateDto(product, brand)));

        Assert.Contains("marca", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static TestUnitOfWork CreateUnitOfWork(Product? product = null)
    {
        var categoryRepository = new Mock<IRepository<Category>>();
        categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId))
            .ReturnsAsync(new Category
            {
                Id = CategoryId,
                Name = "Cuidado Personal",
                Slug = "cuidado-personal",
                IsActive = true
            });

        var products = new Mock<IProductRepository>();
        products.Setup(r => r.SkuExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        products.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync(false);
        products.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);
        products.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        var priceLevels = new Mock<IPriceLevelRepository>();
        priceLevels
            .Setup(r => r.GetByIdAsync(PriceLevelId))
            .ReturnsAsync(new PriceLevel
            {
                Id = PriceLevelId,
                Name = "Retail",
                Priority = 1,
                IsActive = true
            });

        var productPrices = new Mock<IProductPriceRepository>();
        productPrices.Setup(r => r.AddAsync(It.IsAny<ProductPrice>())).ReturnsAsync((ProductPrice p) => p);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(u => u.Products).Returns(products.Object);
        unitOfWork.SetupGet(u => u.PriceLevels).Returns(priceLevels.Object);
        unitOfWork.SetupGet(u => u.ProductPrices).Returns(productPrices.Object);
        unitOfWork.Setup(u => u.Repository<Category>()).Returns(categoryRepository.Object);
        unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        return new TestUnitOfWork(unitOfWork, products);
    }

    private static CreateProductDto CreateCreateDto(string? brand)
    {
        return new CreateProductDto(
            "Desodorante",
            "Desodorante en barra",
            brand,
            "RX-001",
            CategoryId,
            10m,
            false,
            false,
            [
                new CreateProductPriceDto(
                    Guid.Empty,
                    PriceLevelId,
                    82.50m,
                    71.12m,
                    1)
            ],
            null);
    }

    private static UpdateProductDto CreateUpdateDto(Product product, string? brand)
    {
        return new UpdateProductDto(
            product.Id,
            product.Name,
            product.Description,
            brand,
            product.Sku,
            product.CategoryId,
            product.Cost,
            product.IsActive,
            product.HasVirtualStock,
            product.IsFeatured);
    }

    private static Product CreateProduct(Guid? id = null, string? brand = "Rexona")
    {
        var productId = id ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        return new Product
        {
            Id = productId,
            Name = "Desodorante",
            Description = "Desodorante en barra",
            Brand = brand,
            Sku = "RX-001",
            Slug = "desodorante",
            CategoryId = CategoryId,
            Category = new Category { Id = CategoryId, Name = "Cuidado Personal", Slug = "cuidado-personal" },
            Cost = 10m,
            IsActive = true,
            HasVirtualStock = false,
            IsFeatured = false,
            Images = [],
            Prices = []
        };
    }

    private sealed record TestUnitOfWork(
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IProductRepository> Products);
}
