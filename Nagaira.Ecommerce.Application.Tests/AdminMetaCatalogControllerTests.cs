using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Nagaira.Ecommerce.Api.Controllers.Admin;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Application.MetaCatalog;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Integrations.MetaCatalog;
using System.Net;

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
        Assert.True(response.DryRun);
        Assert.Null(response.Success);
        Assert.NotNull(response.Payload);
        Assert.Equal("Router WiFi", response.Payload.Name);
        Assert.Equal("Acme", response.Payload.Brand);
        Assert.Equal("125.50", response.Payload.Price);
        Assert.Equal("HNL", response.Payload.Currency);
        Assert.DoesNotContain("super-secret-token", JsonSerializer.Serialize(response));
    }

    [Fact]
    public async Task TestSync_DryRunFalseInStaging_SubmitsSingleMappingResult()
    {
        var product = CreateProduct();
        MetaCatalogMappingResult? submitted = null;
        var metaClient = new Mock<IMetaCatalogClient>();
        metaClient
            .Setup(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<MetaCatalogMappingResult>, CancellationToken>((items, _) => submitted = items.Single())
            .ReturnsAsync(new MetaCatalogBatchResult([
                new MetaCatalogItemResult(product.Id.ToString("D"), MetaCatalogSyncAction.Upsert, true, "meta-1", null, null, false)
            ]));
        var controller = CreateController(
            product,
            environmentName: "Staging",
            syncEnabled: true,
            catalogId: "catalog-1",
            accessToken: "super-secret-token",
            graphApiVersion: "v25.0",
            metaCatalogClient: metaClient.Object);

        var result = await controller.TestSync(product.Id, dryRun: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogTestSyncResponse>(ok.Value);
        Assert.False(response.DryRun);
        Assert.True(response.Success);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(product.Id.ToString("D"), submitted!.RetailerId);
        Assert.Equal(MetaCatalogSyncAction.Upsert, submitted.Action);
        Assert.DoesNotContain("super-secret-token", JsonSerializer.Serialize(response));
        metaClient.Verify(c => c.SubmitAsync(
            It.Is<IReadOnlyCollection<MetaCatalogMappingResult>>(items => items.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TestSync_DryRunFalseSupportsDelete()
    {
        var product = CreateProduct();
        product.IsDeleted = true;
        MetaCatalogMappingResult? submitted = null;
        var metaClient = new Mock<IMetaCatalogClient>();
        metaClient
            .Setup(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<MetaCatalogMappingResult>, CancellationToken>((items, _) => submitted = items.Single())
            .ReturnsAsync(new MetaCatalogBatchResult([
                new MetaCatalogItemResult(product.Id.ToString("D"), MetaCatalogSyncAction.Delete, true, null, null, null, false)
            ]));
        var controller = CreateController(
            product,
            environmentName: "Development",
            syncEnabled: true,
            catalogId: "catalog-1",
            accessToken: "token",
            graphApiVersion: "v25.0",
            metaCatalogClient: metaClient.Object);

        var result = await controller.TestSync(product.Id, dryRun: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogTestSyncResponse>(ok.Value);
        Assert.Equal(MetaCatalogSyncAction.Delete.ToString(), response.Action);
        Assert.True(response.Success);
        Assert.Equal(MetaCatalogSyncAction.Delete, submitted!.Action);
        Assert.Null(submitted.Item);
    }

    [Fact]
    public async Task TestSync_DryRunFalseInProduction_ReturnsForbiddenWithoutCallingMeta()
    {
        var product = CreateProduct();
        var metaClient = new Mock<IMetaCatalogClient>();
        var controller = CreateController(
            product,
            environmentName: "Production",
            syncEnabled: true,
            catalogId: "catalog-1",
            accessToken: "token",
            graphApiVersion: "v25.0",
            metaCatalogClient: metaClient.Object);

        var result = await controller.TestSync(product.Id, dryRun: false);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        var response = Assert.IsType<MetaCatalogTestSyncResponse>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal(403, response.StatusCode);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TestSync_DryRunFalseWithSyncDisabled_ReturnsBadRequestWithoutCallingMeta()
    {
        var product = CreateProduct();
        var metaClient = new Mock<IMetaCatalogClient>();
        var controller = CreateController(
            product,
            environmentName: "Staging",
            syncEnabled: false,
            catalogId: "catalog-1",
            accessToken: "token",
            graphApiVersion: "v25.0",
            metaCatalogClient: metaClient.Object);

        var result = await controller.TestSync(product.Id, dryRun: false);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogTestSyncResponse>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("SyncEnabled", response.Message);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TestSync_DryRunFalseWithMissingConfiguration_ReturnsBadRequestWithoutCallingMeta()
    {
        var product = CreateProduct();
        var metaClient = new Mock<IMetaCatalogClient>();
        var controller = CreateController(
            product,
            environmentName: "Staging",
            syncEnabled: true,
            catalogId: "",
            accessToken: "token",
            graphApiVersion: "v25.0",
            metaCatalogClient: metaClient.Object);

        var result = await controller.TestSync(product.Id, dryRun: false);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogTestSyncResponse>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("CatalogId", response.Message);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TestSync_DryRunFalseMetaError_ReturnsSanitizedError()
    {
        var product = CreateProduct();
        var metaClient = new Mock<IMetaCatalogClient>();
        metaClient
            .Setup(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MetaCatalogApiException(
                HttpStatusCode.TooManyRequests,
                "Meta Catalog rate limit exceeded.",
                true,
                "4",
                "99",
                "trace-1"));
        var controller = CreateController(
            product,
            environmentName: "Staging",
            syncEnabled: true,
            catalogId: "catalog-1",
            accessToken: "super-secret-token",
            graphApiVersion: "v25.0",
            metaCatalogClient: metaClient.Object);

        var result = await controller.TestSync(product.Id, dryRun: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogTestSyncResponse>(ok.Value);
        Assert.False(response.Success);
        Assert.Equal(429, response.StatusCode);
        Assert.Equal("4", response.ErrorCode);
        Assert.Equal("99", response.ErrorSubcode);
        Assert.True(response.IsTransient);
        Assert.Equal("Meta Catalog rate limit exceeded.", response.Message);
        Assert.Equal("trace-1", response.TraceId);
        Assert.DoesNotContain("super-secret-token", JsonSerializer.Serialize(response));
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

    private static AdminMetaCatalogController CreateController(
        Product? product,
        string accessToken = "",
        string environmentName = "Development",
        bool syncEnabled = false,
        string catalogId = "",
        string graphApiVersion = "",
        IMetaCatalogClient? metaCatalogClient = null)
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
            CatalogId = catalogId,
            AccessToken = accessToken,
            GraphApiVersion = graphApiVersion,
            SyncEnabled = syncEnabled
        });

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);

        metaCatalogClient ??= Mock.Of<IMetaCatalogClient>();

        return new AdminMetaCatalogController(
            unitOfWork.Object,
            options,
            metaCatalogClient,
            environment.Object);
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
