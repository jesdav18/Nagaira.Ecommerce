using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Storage;
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
                new MetaCatalogItemResult(product.Id.ToString("D"), MetaCatalogSyncAction.Upsert, true, "meta-1", null, null, false, "finished", null, ["Image warning"], "batch-handle-1")
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
        Assert.Equal("finished", response.Status);
        Assert.Equal("batch-handle-1", response.BatchHandle);
        Assert.Contains("Image warning", response.Warnings!);
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
        Assert.Null(response.DiagnosticRequestBody);
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
    public async Task TestSync_DryRunFalseMissingHandleInStaging_ReturnsSafeDiagnostics()
    {
        var product = CreateProduct();
        var metaClient = new Mock<IMetaCatalogClient>();
        metaClient
            .Setup(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MetaCatalogBatchResult([
                new MetaCatalogItemResult(
                    product.Id.ToString("D"),
                    MetaCatalogSyncAction.Upsert,
                    false,
                    null,
                    null,
                    "Meta Catalog batch response did not include a handle.",
                    true,
                    "missing_handle",
                    null,
                    null,
                    null,
                    "application/json",
                    125,
                    ["unexpected"],
                    """{"unexpected":true,"message":"[redacted]"}""",
                    """{"item_type":"PRODUCT_ITEM","requests":[{"method":"CREATE","data":{"id":"11111111-1111-1111-1111-111111111111"}}]}""")
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
        Assert.False(response.Success);
        Assert.Equal("missing_handle", response.Status);
        Assert.Equal("application/json", response.ResponseContentType);
        Assert.Equal(125, response.ResponseBodyLength);
        Assert.Contains("unexpected", response.ResponseTopLevelProperties!);
        Assert.Contains("[redacted]", response.DiagnosticResponseBody);
        Assert.Contains(@"""id"":""11111111-1111-1111-1111-111111111111""", response.DiagnosticRequestBody);
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

    [Fact]
    public async Task SyncPlan_InProduction_ReturnsForbidden()
    {
        var metaClient = new Mock<IMetaCatalogClient>();
        var controller = CreateController(
            CreateProduct(),
            environmentName: "Production",
            metaCatalogClient: metaClient.Object);

        var result = await controller.SyncPlan();

        var statusCode = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(403, statusCode.StatusCode);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncPlan_LimitIsCappedAtTwoHundredAndDoesNotCallMeta()
    {
        var product = CreateProduct();
        var metaClient = new Mock<IMetaCatalogClient>();
        var controller = CreateController(
            product,
            environmentName: "Staging",
            metaCatalogClient: metaClient.Object);

        var result = await controller.SyncPlan(limit: 500);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogSyncPlanResponse>(ok.Value);
        Assert.True(response.DryRun);
        Assert.Equal(200, response.Limit);
        Assert.Equal(1, response.Summary.Scanned);
        Assert.Equal(MetaCatalogSyncPlanOperations.Create, Assert.Single(response.Items).Operation);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BrandBackfillPlan_InProduction_ReturnsForbidden()
    {
        var metaClient = new Mock<IMetaCatalogClient>();
        var controller = CreateController(
            CreateProduct(),
            environmentName: "Production",
            metaCatalogClient: metaClient.Object);

        var result = await controller.BrandBackfillPlan();

        var statusCode = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(403, statusCode.StatusCode);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BrandBackfillPlan_LimitIsCappedAndDoesNotCallMetaOrSaveChanges()
    {
        var product = CreateProduct();
        product.Brand = null;
        var supplier = new ProductSupplier
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            SupplierId = Guid.NewGuid(),
            IsActive = true,
            IsPrimary = true,
            Priority = 1,
            Supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                Name = "Distribuidora Central",
                IsActive = true
            }
        };
        var metaClient = new Mock<IMetaCatalogClient>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var controller = CreateController(
            product,
            environmentName: "Staging",
            metaCatalogClient: metaClient.Object,
            productSuppliers: [supplier],
            unitOfWorkMock: unitOfWork);

        var result = await controller.BrandBackfillPlan(limit: 500);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogBrandBackfillPlanResponse>(ok.Value);
        Assert.True(response.DryRun);
        Assert.Equal(200, response.Limit);
        var item = Assert.Single(response.Items);
        Assert.Equal(MetaCatalogBrandBackfillPlanOperations.Skipped, item.Operation);
        Assert.Null(item.SuggestedBrand);
        Assert.Equal(MetaCatalogBrandBackfillConfidence.None, item.Confidence);
        Assert.Equal("brand_not_recognized", item.Reason);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task BrandBackfill_DryRunDoesNotSaveChangesOrCallMeta()
    {
        var product = CreateProduct(name: "Desodorante Rexona Clinical", brand: null);
        var metaClient = new Mock<IMetaCatalogClient>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var controller = CreateController(
            product,
            environmentName: "Staging",
            metaCatalogClient: metaClient.Object,
            unitOfWorkMock: unitOfWork);

        var result = await controller.BrandBackfill(dryRun: true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogBrandBackfillResponse>(ok.Value);
        var item = Assert.Single(response.Items);
        Assert.True(response.DryRun);
        Assert.Equal(MetaCatalogBrandBackfillApplyOperations.Updated, item.Operation);
        Assert.Equal("Rexona", item.NewBrand);
        Assert.Null(product.Brand);
        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BrandBackfill_DryRunFalseAppliesHighConfidenceSuggestionAndSavesOnce()
    {
        var product = CreateProduct(name: "Desodorante Rexona Clinical", brand: null);
        var metaClient = new Mock<IMetaCatalogClient>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var controller = CreateController(
            product,
            environmentName: "Staging",
            metaCatalogClient: metaClient.Object,
            unitOfWorkMock: unitOfWork);

        var result = await controller.BrandBackfill(dryRun: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogBrandBackfillResponse>(ok.Value);
        var item = Assert.Single(response.Items);
        Assert.False(response.DryRun);
        Assert.Equal(MetaCatalogBrandBackfillApplyOperations.Updated, item.Operation);
        Assert.Null(item.PreviousBrand);
        Assert.Equal("Rexona", item.NewBrand);
        Assert.Equal("Rexona", product.Brand);
        Assert.Equal(1, response.Summary.Updated);
        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BrandBackfill_DryRunFalseDoesNotApplyNoneConfidence()
    {
        var product = CreateProduct(name: "Desodorante Genérico", brand: null);
        var metaClient = new Mock<IMetaCatalogClient>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var controller = CreateController(
            product,
            environmentName: "Staging",
            metaCatalogClient: metaClient.Object,
            productSuppliers:
            [
                CreateProductSupplier(product.Id, "Distribuidora Central")
            ],
            unitOfWorkMock: unitOfWork);

        var result = await controller.BrandBackfill(dryRun: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogBrandBackfillResponse>(ok.Value);
        var item = Assert.Single(response.Items);
        Assert.Equal(MetaCatalogBrandBackfillApplyOperations.Skipped, item.Operation);
        Assert.Equal("brand_not_recognized", item.Reason);
        Assert.Null(product.Brand);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BrandBackfill_DryRunFalseDoesNotOverwriteRealBrand()
    {
        var product = CreateProduct(name: "Desodorante Rexona Clinical", brand: "Acme");
        var metaClient = new Mock<IMetaCatalogClient>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var controller = CreateController(
            product,
            environmentName: "Staging",
            metaCatalogClient: metaClient.Object,
            unitOfWorkMock: unitOfWork);

        var result = await controller.BrandBackfill(dryRun: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogBrandBackfillResponse>(ok.Value);
        var item = Assert.Single(response.Items);
        Assert.Equal(MetaCatalogBrandBackfillApplyOperations.Unchanged, item.Operation);
        Assert.Equal("Acme", item.PreviousBrand);
        Assert.Equal("Acme", product.Brand);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BrandBackfill_DryRunFalseReplacesTemporaryBrand()
    {
        var product = CreateProduct(name: "Desodorante Rexona Clinical", brand: "Nagaira Test");
        var metaClient = new Mock<IMetaCatalogClient>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var controller = CreateController(
            product,
            environmentName: "Staging",
            metaCatalogClient: metaClient.Object,
            unitOfWorkMock: unitOfWork);

        var result = await controller.BrandBackfill(dryRun: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogBrandBackfillResponse>(ok.Value);
        var item = Assert.Single(response.Items);
        Assert.Equal(MetaCatalogBrandBackfillApplyOperations.Updated, item.Operation);
        Assert.Equal("Nagaira Test", item.PreviousBrand);
        Assert.Equal("Rexona", item.NewBrand);
        Assert.Equal("Rexona", product.Brand);
    }

    [Fact]
    public async Task BrandBackfill_DryRunFalseSecondRunIsIdempotent()
    {
        var product = CreateProduct(name: "Desodorante Rexona Clinical", brand: null);
        var metaClient = new Mock<IMetaCatalogClient>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var controller = CreateController(
            product,
            environmentName: "Staging",
            metaCatalogClient: metaClient.Object,
            unitOfWorkMock: unitOfWork);

        await controller.BrandBackfill(dryRun: false);
        var secondResult = await controller.BrandBackfill(dryRun: false);

        var ok = Assert.IsType<OkObjectResult>(secondResult.Result);
        var response = Assert.IsType<MetaCatalogBrandBackfillResponse>(ok.Value);
        var item = Assert.Single(response.Items);
        Assert.Equal(MetaCatalogBrandBackfillApplyOperations.Unchanged, item.Operation);
        Assert.Equal("Rexona", item.PreviousBrand);
        Assert.Null(item.NewBrand);
        Assert.Equal("Rexona", product.Brand);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BrandBackfill_DryRunFalseSkipsWhenBrandChangedSincePlan()
    {
        var plannedProduct = CreateProduct(name: "Desodorante Rexona Clinical", brand: null);
        var currentProduct = CreateProduct(name: "Desodorante Rexona Clinical", brand: "Acme");
        var metaClient = new Mock<IMetaCatalogClient>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var controller = CreateController(
            plannedProduct,
            environmentName: "Staging",
            metaCatalogClient: metaClient.Object,
            unitOfWorkMock: unitOfWork,
            currentProduct: currentProduct);

        var result = await controller.BrandBackfill(dryRun: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MetaCatalogBrandBackfillResponse>(ok.Value);
        var item = Assert.Single(response.Items);
        Assert.Equal(MetaCatalogBrandBackfillApplyOperations.Skipped, item.Operation);
        Assert.Equal("brand_changed_since_plan", item.Reason);
        Assert.Equal("Acme", item.PreviousBrand);
        Assert.Null(item.NewBrand);
        Assert.Equal("Acme", currentProduct.Brand);
    }

    [Fact]
    public async Task BrandBackfill_InProduction_ReturnsForbidden()
    {
        var metaClient = new Mock<IMetaCatalogClient>();
        var controller = CreateController(
            CreateProduct(name: "Desodorante Rexona Clinical", brand: null),
            environmentName: "Production",
            metaCatalogClient: metaClient.Object);

        var result = await controller.BrandBackfill(dryRun: false);

        var statusCode = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(403, statusCode.StatusCode);
        metaClient.Verify(c => c.SubmitAsync(It.IsAny<IReadOnlyCollection<MetaCatalogMappingResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AdminMetaCatalogController CreateController(
        Product? product,
        string accessToken = "",
        string environmentName = "Development",
        bool syncEnabled = false,
        string catalogId = "",
        string graphApiVersion = "",
        IMetaCatalogClient? metaCatalogClient = null,
        IReadOnlyList<ProductSupplier>? productSuppliers = null,
        Mock<IUnitOfWork>? unitOfWorkMock = null,
        Product? currentProduct = null)
    {
        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(r => r.GetByIdIncludingDeletedAsync(It.IsAny<Guid>()))
            .ReturnsAsync(currentProduct ?? product);
        productRepository
            .Setup(r => r.GetMetaCatalogSyncPlanCandidatesAsync(It.IsAny<int>()))
            .ReturnsAsync(product == null ? [] : [product]);
        productRepository
            .Setup(r => r.GetMetaCatalogBrandBackfillPlanCandidatesAsync(It.IsAny<int>()))
            .ReturnsAsync(product == null ? [] : [product]);

        var syncStateRepository = new Mock<IMetaProductSyncStateRepository>();
        syncStateRepository
            .Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        var productSupplierRepository = new Mock<IProductSupplierRepository>();
        productSupplierRepository
            .Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(productSuppliers ?? []);

        var unitOfWork = unitOfWorkMock ?? new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);
        unitOfWork.SetupGet(u => u.MetaProductSyncStates).Returns(syncStateRepository.Object);
        unitOfWork.SetupGet(u => u.ProductSuppliers).Returns(productSupplierRepository.Object);
        unitOfWork
            .Setup(u => u.BeginTransactionAsync())
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());
        unitOfWork
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);
        unitOfWork
            .Setup(u => u.CommitTransactionAsync())
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.RollbackTransactionAsync())
            .Returns(Task.CompletedTask);

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

    private static Product CreateProduct(string name = " Router WiFi ", string? brand = " Acme ")
    {
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        return new Product
        {
            Id = productId,
            Name = name,
            Description = " Router para casa ",
            Brand = brand,
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

    private static ProductSupplier CreateProductSupplier(Guid productId, string supplierName)
    {
        var supplierId = Guid.NewGuid();
        return new ProductSupplier
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            SupplierId = supplierId,
            IsActive = true,
            IsDeleted = false,
            IsPrimary = true,
            Priority = 1,
            Supplier = new Supplier
            {
                Id = supplierId,
                Name = supplierName,
                IsActive = true,
                IsDeleted = false
            }
        };
    }
}
