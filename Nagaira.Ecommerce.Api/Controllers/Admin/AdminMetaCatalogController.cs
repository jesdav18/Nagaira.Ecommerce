using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Application.MetaCatalog;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Integrations.MetaCatalog;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/meta-catalog")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminMetaCatalogController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly MetaCatalogOptions _options;
    private readonly IMetaCatalogClient _metaCatalogClient;
    private readonly IWebHostEnvironment _environment;

    public AdminMetaCatalogController(
        IUnitOfWork unitOfWork,
        IOptions<MetaCatalogOptions> options,
        IMetaCatalogClient metaCatalogClient,
        IWebHostEnvironment environment)
    {
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _metaCatalogClient = metaCatalogClient;
        _environment = environment;
    }

    [HttpPost("products/{productId:guid}/test-sync")]
    public async Task<ActionResult<MetaCatalogTestSyncResponse>> TestSync(
        Guid productId,
        [FromQuery] bool dryRun = true)
    {
        var product = await _unitOfWork.Products.GetByIdIncludingDeletedAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        var result = MetaCatalogProductMapper.Map(product, _options);
        if (dryRun)
        {
            return Ok(MetaCatalogTestSyncResponse.FromDryRun(result));
        }

        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            return StatusCode(StatusCodes.Status403Forbidden, MetaCatalogTestSyncResponse.FromBlocked(
                result,
                StatusCodes.Status403Forbidden,
                "Meta Catalog test sync with dryRun=false is only allowed in Development or Staging."));
        }

        var validationMessage = ValidateLiveTestConfiguration();
        if (validationMessage != null)
        {
            return BadRequest(MetaCatalogTestSyncResponse.FromBlocked(
                result,
                StatusCodes.Status400BadRequest,
                validationMessage));
        }

        try
        {
            var cancellationToken = ControllerContext.HttpContext?.RequestAborted ?? CancellationToken.None;
            var submitResult = await _metaCatalogClient.SubmitAsync([result], cancellationToken);
            var itemResult = submitResult.Items.FirstOrDefault(i => i.RetailerId == result.RetailerId);
            if (itemResult == null)
            {
                return Ok(MetaCatalogTestSyncResponse.FromLiveResult(
                    result,
                    false,
                    StatusCodes.Status200OK,
                    null,
                    null,
                    false,
                    "Meta Catalog did not return a per-product result.",
                    null,
                    null,
                    null,
                    null));
            }

            return Ok(MetaCatalogTestSyncResponse.FromLiveResult(
                result,
                itemResult.Success,
                StatusCodes.Status200OK,
                itemResult.ErrorCode,
                itemResult.ErrorSubcode,
                itemResult.IsTransient,
                itemResult.ErrorMessage,
                null,
                itemResult.Status,
                itemResult.Warnings,
                itemResult.BatchHandle,
                _environment.IsDevelopment() || _environment.IsStaging()
                    ? itemResult.ResponseContentType
                    : null,
                _environment.IsDevelopment() || _environment.IsStaging()
                    ? itemResult.ResponseBodyLength
                    : null,
                _environment.IsDevelopment() || _environment.IsStaging()
                    ? itemResult.ResponseTopLevelProperties
                    : null,
                _environment.IsDevelopment() || _environment.IsStaging()
                    ? itemResult.DiagnosticResponseBody
                    : null,
                _environment.IsDevelopment() || _environment.IsStaging()
                    ? itemResult.DiagnosticRequestBody
                    : null));
        }
        catch (MetaCatalogApiException ex)
        {
            return Ok(MetaCatalogTestSyncResponse.FromLiveResult(
                result,
                false,
                ex.HttpStatusCode.HasValue ? (int)ex.HttpStatusCode.Value : null,
                ex.MetaErrorCode,
                ex.MetaErrorSubcode,
                ex.IsTransient,
                ex.SafeMessage,
                ex.RequestId,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }
    }

    [HttpPost("sync-plan")]
    public async Task<ActionResult<MetaCatalogSyncPlanResponse>> SyncPlan([FromQuery] int limit = 50)
    {
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var safeLimit = Math.Clamp(limit, 1, 200);
        var products = await _unitOfWork.Products.GetMetaCatalogSyncPlanCandidatesAsync(safeLimit);
        var productIds = products.Select(p => p.Id).ToList();
        var states = await _unitOfWork.MetaProductSyncStates.GetByProductIdsAsync(productIds);
        var statesByProductId = states
            .GroupBy(s => s.ProductId)
            .ToDictionary(g => g.Key, g => g.First());

        return Ok(MetaCatalogSyncPlanner.BuildPlan(products, statesByProductId, _options, safeLimit));
    }

    [HttpPost("brand-backfill-plan")]
    public async Task<ActionResult<MetaCatalogBrandBackfillPlanResponse>> BrandBackfillPlan([FromQuery] int limit = 200)
    {
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var safeLimit = Math.Clamp(limit, 1, 200);
        var products = await _unitOfWork.Products.GetMetaCatalogBrandBackfillPlanCandidatesAsync(safeLimit);
        var productIds = products.Select(p => p.Id).ToList();
        var productSuppliers = await _unitOfWork.ProductSuppliers.GetByProductIdsAsync(productIds);
        var suppliersByProductId = productSuppliers
            .GroupBy(ps => ps.ProductId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<ProductSupplier>)g.ToList());

        return Ok(MetaCatalogBrandBackfillPlanner.BuildPlan(products, suppliersByProductId, safeLimit));
    }

    [HttpPost("brand-backfill")]
    public async Task<ActionResult<MetaCatalogBrandBackfillResponse>> BrandBackfill(
        [FromQuery] int limit = 200,
        [FromQuery] bool dryRun = true)
    {
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var safeLimit = Math.Clamp(limit, 1, 500);
        var products = await _unitOfWork.Products.GetMetaCatalogBrandBackfillPlanCandidatesAsync(safeLimit);
        var productIds = products.Select(p => p.Id).ToList();
        var productSuppliers = await _unitOfWork.ProductSuppliers.GetByProductIdsAsync(productIds);
        var suppliersByProductId = productSuppliers
            .GroupBy(ps => ps.ProductId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<ProductSupplier>)g.ToList());
        var plan = MetaCatalogBrandBackfillPlanner.BuildPlan(products, suppliersByProductId, safeLimit, maxLimit: 500);

        if (dryRun)
        {
            var dryRunItems = plan.Items
                .Select(item => new MetaCatalogBrandBackfillItem(
                    item.ProductId,
                    item.Name,
                    item.CurrentBrand,
                    item.SuggestedBrand,
                    ToDryRunApplyOperation(item),
                    item.Reason))
                .ToList();

            return Ok(new MetaCatalogBrandBackfillResponse(
                true,
                MetaCatalogBrandBackfillSummary.FromItems(dryRunItems),
                dryRunItems));
        }

        var appliedItems = new List<MetaCatalogBrandBackfillItem>();
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            foreach (var planItem in plan.Items)
            {
                if (!IsApplicablePlanItem(planItem))
                {
                    appliedItems.Add(new MetaCatalogBrandBackfillItem(
                        planItem.ProductId,
                        planItem.Name,
                        planItem.CurrentBrand,
                        planItem.SuggestedBrand,
                        ToSkippedOrUnchangedOperation(planItem),
                        planItem.Reason));
                    continue;
                }

                var currentProduct = await _unitOfWork.Products.GetByIdIncludingDeletedAsync(planItem.ProductId);
                if (currentProduct == null)
                {
                    appliedItems.Add(new MetaCatalogBrandBackfillItem(
                        planItem.ProductId,
                        planItem.Name,
                        planItem.CurrentBrand,
                        planItem.SuggestedBrand,
                        MetaCatalogBrandBackfillApplyOperations.Skipped,
                        "product_not_found"));
                    continue;
                }

                var previousBrand = NormalizeBrandForBackfill(currentProduct.Brand);
                if (!MetaCatalogBrandBackfillPlanner.CanReplaceBrandValue(previousBrand))
                {
                    appliedItems.Add(new MetaCatalogBrandBackfillItem(
                        currentProduct.Id,
                        currentProduct.Name,
                        previousBrand,
                        null,
                        MetaCatalogBrandBackfillApplyOperations.Skipped,
                        "brand_changed_since_plan"));
                    continue;
                }

                var currentSuppliers = suppliersByProductId.TryGetValue(currentProduct.Id, out var suppliers)
                    ? suppliers
                    : [];
                var currentPlan = MetaCatalogBrandBackfillPlanner.BuildPlan(
                    [currentProduct],
                    new Dictionary<Guid, IReadOnlyCollection<ProductSupplier>>
                    {
                        [currentProduct.Id] = currentSuppliers
                    },
                    1);
                var currentPlanItem = currentPlan.Items.Single();

                if (!IsApplicablePlanItem(currentPlanItem))
                {
                    appliedItems.Add(new MetaCatalogBrandBackfillItem(
                        currentProduct.Id,
                        currentProduct.Name,
                        previousBrand,
                        currentPlanItem.SuggestedBrand,
                        MetaCatalogBrandBackfillApplyOperations.Skipped,
                        currentPlanItem.Reason));
                    continue;
                }

                var newBrand = NormalizeBrandForBackfill(currentPlanItem.SuggestedBrand);
                if (newBrand == null || newBrand.Length > 255)
                {
                    appliedItems.Add(new MetaCatalogBrandBackfillItem(
                        currentProduct.Id,
                        currentProduct.Name,
                        previousBrand,
                        currentPlanItem.SuggestedBrand,
                        MetaCatalogBrandBackfillApplyOperations.Skipped,
                        "invalid_suggested_brand"));
                    continue;
                }

                currentProduct.Brand = newBrand;
                currentProduct.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Products.UpdateAsync(currentProduct);
                appliedItems.Add(new MetaCatalogBrandBackfillItem(
                    currentProduct.Id,
                    currentProduct.Name,
                    previousBrand,
                    newBrand,
                    MetaCatalogBrandBackfillApplyOperations.Updated,
                    currentPlanItem.Reason));
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return Ok(new MetaCatalogBrandBackfillResponse(
            false,
            MetaCatalogBrandBackfillSummary.FromItems(appliedItems),
            appliedItems));
    }

    private string? ValidateLiveTestConfiguration()
    {
        if (!_options.SyncEnabled)
        {
            return "MetaCatalog:SyncEnabled must be true for dryRun=false.";
        }

        if (string.IsNullOrWhiteSpace(_options.CatalogId))
        {
            return "MetaCatalog:CatalogId is required for dryRun=false.";
        }

        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            return "MetaCatalog:AccessToken is required for dryRun=false.";
        }

        if (string.IsNullOrWhiteSpace(_options.GraphApiVersion))
        {
            return "MetaCatalog:GraphApiVersion is required for dryRun=false.";
        }

        return null;
    }

    private static string ToDryRunApplyOperation(MetaCatalogBrandBackfillPlanItem item)
    {
        return string.Equals(item.Operation, MetaCatalogBrandBackfillPlanOperations.Update, StringComparison.Ordinal)
            && string.Equals(item.Confidence, MetaCatalogBrandBackfillConfidence.High, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(item.SuggestedBrand)
                ? MetaCatalogBrandBackfillApplyOperations.Updated
                : ToSkippedOrUnchangedOperation(item);
    }

    private static string ToSkippedOrUnchangedOperation(MetaCatalogBrandBackfillPlanItem item)
    {
        return string.Equals(item.Operation, MetaCatalogBrandBackfillPlanOperations.Unchanged, StringComparison.Ordinal)
            ? MetaCatalogBrandBackfillApplyOperations.Unchanged
            : MetaCatalogBrandBackfillApplyOperations.Skipped;
    }

    private static bool IsApplicablePlanItem(MetaCatalogBrandBackfillPlanItem item)
    {
        return string.Equals(item.Operation, MetaCatalogBrandBackfillPlanOperations.Update, StringComparison.Ordinal)
            && string.Equals(item.Confidence, MetaCatalogBrandBackfillConfidence.High, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(item.SuggestedBrand);
    }

    private static string? NormalizeBrandForBackfill(string? brand)
    {
        var normalized = brand?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

}

public record MetaCatalogTestSyncResponse(
    string RetailerId,
    string Action,
    string PayloadHash,
    MetaCatalogProduct? Payload,
    bool DryRun,
    bool? Success,
    int? StatusCode,
    string? ErrorCode,
    string? ErrorSubcode,
    bool? IsTransient,
    string? Message,
    string? TraceId,
    string? Status,
    IReadOnlyList<string>? Warnings,
    string? BatchHandle,
    string? ResponseContentType,
    int? ResponseBodyLength,
    IReadOnlyList<string>? ResponseTopLevelProperties,
    string? DiagnosticResponseBody,
    string? DiagnosticRequestBody)
{
    public static MetaCatalogTestSyncResponse FromDryRun(MetaCatalogMappingResult result)
    {
        return new MetaCatalogTestSyncResponse(
            result.RetailerId,
            result.Action.ToString(),
            result.PayloadHash,
            result.Item,
            true,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    public static MetaCatalogTestSyncResponse FromBlocked(
        MetaCatalogMappingResult result,
        int statusCode,
        string message)
    {
        return new MetaCatalogTestSyncResponse(
            result.RetailerId,
            result.Action.ToString(),
            result.PayloadHash,
            null,
            false,
            false,
            statusCode,
            null,
            null,
            false,
            message,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    public static MetaCatalogTestSyncResponse FromLiveResult(
        MetaCatalogMappingResult result,
        bool success,
        int? statusCode,
        string? errorCode,
        string? errorSubcode,
        bool isTransient,
        string? message,
        string? traceId,
        string? status,
        IReadOnlyList<string>? warnings,
        string? batchHandle,
        string? responseContentType = null,
        int? responseBodyLength = null,
        IReadOnlyList<string>? responseTopLevelProperties = null,
        string? diagnosticResponseBody = null,
        string? diagnosticRequestBody = null)
    {
        return new MetaCatalogTestSyncResponse(
            result.RetailerId,
            result.Action.ToString(),
            result.PayloadHash,
            null,
            false,
            success,
            statusCode,
            errorCode,
            errorSubcode,
            isTransient,
            message,
            traceId,
            status,
            warnings,
            batchHandle,
            responseContentType,
            responseBodyLength,
            responseTopLevelProperties,
            diagnosticResponseBody,
            diagnosticRequestBody);
    }
}
