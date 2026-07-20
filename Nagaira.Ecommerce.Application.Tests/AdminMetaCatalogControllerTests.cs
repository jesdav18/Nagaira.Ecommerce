using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Nagaira.Ecommerce.Api.Controllers.Admin;
using Nagaira.Ecommerce.Application.MetaCatalog;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Tests;

public class AdminMetaCatalogControllerTests
{
    private static readonly Guid RetailPriceLevelId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task TestSync_ProductNotFound_ReturnsNotFound()
    {
        var controller = CreateController(null);

        var result = await controller.TestSync(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task TestSync_DefaultDryRun_ReturnsSanitizedUpsertPayloadWithoutCallingMeta()
    {
        var product = CreateProduct();
        var controller = CreateController(product, accessToken: "super-secret-token");

        var result = await controller.TestSync(product.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogTestSyncResponse>(ok.Value);
        Assert.Equal(product.Id.ToString("D"), response.RetailerId);
        Assert.Equal(MetaCatalogSyncAction.Upsert.ToString(), response.Action);
        Assert.NotNull(response.Payload);
        Assert.Equal("Router WiFi", response.Payload.Name);
        Assert.Equal("Acme", response.Payload.Brand);
        Assert.Equal("125.50", response.Payload.Price);
        Assert.Equal("HNL", response.Payload.Currency);
        Assert.DoesNotContain("super-secret-token", JsonSerializer.Serialize(response));
    }

    [Fact]
    public async Task TestSync_DryRunFalse_ReturnsBadRequest()
    {
        var product = CreateProduct();
        var controller = CreateController(product);

        var result = await controller.TestSync(product.Id, dryRun: false);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task TestSync_InactiveProduct_ReturnsDeleteWithoutPayload()
    {
        var product = CreateProduct();
        product.IsActive = false;
        var controller = CreateController(product);

        var result = await controller.TestSync(product.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogTestSyncResponse>(ok.Value);
        Assert.Equal(MetaCatalogSyncAction.Delete.ToString(), response.Action);
        Assert.Equal(product.Id.ToString("D"), response.RetailerId);
        Assert.Null(response.Payload);
        Assert.False(string.IsNullOrWhiteSpace(response.PayloadHash));
    }

    [Fact]
    public async Task TestSync_DeletedProduct_ReturnsDeleteWithoutPayload()
    {
        var product = CreateProduct();
        product.IsDeleted = true;
        var controller = CreateController(product);

        var result = await controller.TestSync(product.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogTestSyncResponse>(ok.Value);
        Assert.Equal(MetaCatalogSyncAction.Delete.ToString(), response.Action);
        Assert.Equal(product.Id.ToString("D"), response.RetailerId);
        Assert.Null(response.Payload);
    }

    private static AdminMetaCatalogController CreateController(Product? product, string accessToken = "")
    {
        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(r => r.GetByIdIncludingDeletedAsync(It.IsAny<Guid>()))
            .ReturnsAsync(product);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);

        var options = Options.Create(new MetaCatalogOptions
        {
            Currency = "HNL",
            PublicBaseUrl = "https://store.example",
            PublicPriceLevelId = RetailPriceLevelId,
            AccessToken = accessToken,
            SyncEnabled = false
        });

        return new AdminMetaCatalogController(unitOfWork.Object, options);
    }

    private static Product CreateProduct()
    {
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        return new Product
        {
            Id = productId,
            Name = " Router WiFi ",
            Description = " Router para casa ",
            Brand = " Acme ",
            Sku = "RTR-001",
            Slug = "router-wifi",
            IsActive = true,
            IsDeleted = false,
            CategoryId = Guid.NewGuid(),
            Category = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Redes",
                Slug = "redes",
                IsActive = true
            },
            InventoryBalance = new InventoryBalance
            {
                ProductId = productId,
                AvailableQuantity = 5,
                ReservedQuantity = 0,
                LastUpdatedAt = DateTime.UtcNow
            },
            Images =
            [
                new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    ImageUrl = "https://cdn.example/router-primary.jpg",
                    DisplayOrder = 10,
                    IsPrimary = true
                }
            ],
            Prices =
            [
                new ProductPrice
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    PriceLevelId = RetailPriceLevelId,
                    Price = 125.50m,
                    MinQuantity = 1,
                    IsActive = true
                }
            ]
        };
    }
}
