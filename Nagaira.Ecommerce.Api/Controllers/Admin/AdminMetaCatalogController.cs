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

    [HttpPost("sync")]
    public async Task<ActionResult<MetaCatalogSyncExecutionResponse>> Sync(
        [FromQuery] int limit = 20,
        [FromQuery] bool dryRun = true)
    {
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var safeLimit = Math.Clamp(limit, 1, 50);
        var executionLimit = Math.Min(safeLimit, 20);
        var products = await _unitOfWork.Products.GetMetaCatalogSyncPlanCandidatesAsync(executionLimit);
        var productIds = products.Select(p => p.Id).ToList();
        var states = await _unitOfWork.MetaProductSyncStates.GetByProductIdsAsync(productIds);
        var statesByProductId = states
            .GroupBy(s => s.ProductId)
            .ToDictionary(g => g.Key, g => g.First());
        var plan = MetaCatalogSyncPlanner.BuildPlan(products, statesByProductId, _options, executionLimit);

        if (dryRun)
        {
            var dryRunItems = plan.Items.Select(CreateDryRunSyncItem).ToList();
            return Ok(new MetaCatalogSyncExecutionResponse(
                true,
                MetaCatalogSyncExecutionSummary.FromItems(
                    dryRunItems,
                    plan.Items.Count(IsExecutableSyncOperation)),
                dryRunItems));
        }

        var validationMessage = ValidateLiveTestConfiguration();
        if (validationMessage != null)
        {
            return BadRequest(new MetaCatalogSyncExecutionResponse(
                false,
                new MetaCatalogSyncExecutionSummary(plan.Summary.Scanned, 0, 0, 0, 0, 0, plan.Items.Count),
                plan.Items.Select(item => new MetaCatalogSyncExecutionItem(
                    item.ProductId,
                    item.Name,
                    item.Operation,
                    MetaCatalogSyncExecutionStatuses.Skipped,
                    item.PayloadHash,
                    null,
                    validationMessage)).ToList()));
        }

        var executablePlanItems = plan.Items
            .Where(IsExecutableSyncOperation)
            .Take(executionLimit)
            .ToList();
        var executableIds = executablePlanItems.Select(i => i.ProductId).ToList();
        var currentProducts = await _unitOfWork.Products.GetByIdsForMetaCatalogSyncAsync(executableIds);
        var currentProductsById = currentProducts.ToDictionary(p => p.Id);
        var mappingsByProductId = new Dictionary<Guid, MetaCatalogMappingResult>();
        var responseItems = new List<MetaCatalogSyncExecutionItem>();

        foreach (var planItem in plan.Items)
        {
            if (!IsExecutableSyncOperation(planItem))
            {
                responseItems.Add(CreateNonSubmittedSyncItem(planItem));
                continue;
            }

            if (!currentProductsById.TryGetValue(planItem.ProductId, out var currentProduct))
            {
                responseItems.Add(new MetaCatalogSyncExecutionItem(
                    planItem.ProductId,
                    planItem.Name,
                    planItem.Operation,
                    MetaCatalogSyncExecutionStatuses.Skipped,
                    planItem.PayloadHash,
                    null,
                    "product_not_found"));
                continue;
            }

            var currentOutcome = MetaCatalogProductMapper.TryMap(currentProduct, _options);
            if (currentOutcome.MappingResult == null)
            {
                responseItems.Add(new MetaCatalogSyncExecutionItem(
                    planItem.ProductId,
                    currentProduct.Name,
                    MetaCatalogSyncPlanOperations.Skipped,
                    MetaCatalogSyncExecutionStatuses.Skipped,
                    null,
                    null,
                    currentOutcome.Reason));
                continue;
            }

            if (!string.Equals(planItem.PayloadHash, currentOutcome.MappingResult.PayloadHash, StringComparison.Ordinal))
            {
                responseItems.Add(new MetaCatalogSyncExecutionItem(
                    planItem.ProductId,
                    currentProduct.Name,
                    planItem.Operation,
                    MetaCatalogSyncExecutionStatuses.Skipped,
                    currentOutcome.MappingResult.PayloadHash,
                    null,
                    "product_changed_since_plan"));
                continue;
            }

            mappingsByProductId[planItem.ProductId] = currentOutcome.MappingResult;
        }

        if (mappingsByProductId.Count > 0)
        {
            var cancellationToken = ControllerContext.HttpContext?.RequestAborted ?? CancellationToken.None;
            var submitResult = await _metaCatalogClient.SubmitAsync(mappingsByProductId.Values.ToList(), cancellationToken);
            var resultsByRetailerId = submitResult.Items
                .GroupBy(i => i.RetailerId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First());
            var now = DateTime.UtcNow;
            var statesById = statesByProductId;

            foreach (var (productId, mapping) in mappingsByProductId)
            {
                var planItem = executablePlanItems.First(i => i.ProductId == productId);
                resultsByRetailerId.TryGetValue(mapping.RetailerId, out var itemResult);
                var (state, isNewState) = GetOrCreateSyncState(statesById, productId, mapping.RetailerId, now);
                if (isNewState)
                {
                    await _unitOfWork.MetaProductSyncStates.AddAsync(state);
                }
                ApplyMetaResultToState(state, mapping, itemResult, now);

                responseItems.Add(new MetaCatalogSyncExecutionItem(
                    productId,
                    planItem.Name,
                    planItem.Operation,
                    ToExecutionStatus(itemResult),
                    mapping.PayloadHash,
                    itemResult?.BatchHandle,
                    itemResult?.ErrorMessage));
            }

            await _unitOfWork.SaveChangesAsync();
        }

        var orderedItems = plan.Items
            .Select(planItem => responseItems.First(i => i.ProductId == planItem.ProductId))
            .ToList();
        return Ok(new MetaCatalogSyncExecutionResponse(
            false,
            MetaCatalogSyncExecutionSummary.FromItems(orderedItems),
            orderedItems));
    }

    [HttpPost("reconcile")]
    public async Task<ActionResult<MetaCatalogSyncExecutionResponse>> Reconcile([FromQuery] int limit = 50)
    {
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var validationMessage = ValidateLiveTestConfiguration();
        if (validationMessage != null)
        {
            return BadRequest(new MetaCatalogSyncExecutionResponse(
                false,
                new MetaCatalogSyncExecutionSummary(0, 0, 0, 0, 0, 0, 0),
                []));
        }

        var safeLimit = Math.Clamp(limit, 1, 50);
        var states = await _unitOfWork.MetaProductSyncStates.GetProcessingWithBatchHandleAsync(safeLimit);
        var responseItems = new List<MetaCatalogSyncExecutionItem>();
        var cancellationToken = ControllerContext.HttpContext?.RequestAborted ?? CancellationToken.None;
        var now = DateTime.UtcNow;

        foreach (var state in states)
        {
            var action = ParseSyncAction(state.LastAction);
            var mapping = new MetaCatalogMappingResult(
                action,
                state.RetailerId,
                null,
                state.PendingPayloadHash ?? state.LastPayloadHash ?? string.Empty);
            var result = await _metaCatalogClient.CheckBatchStatusAsync([mapping], state.BatchHandle!, cancellationToken);
            var itemResult = result.Items.FirstOrDefault(i => string.Equals(i.RetailerId, state.RetailerId, StringComparison.Ordinal));
            ApplyMetaResultToState(state, mapping, itemResult, now);

            responseItems.Add(new MetaCatalogSyncExecutionItem(
                state.ProductId,
                string.Empty,
                ToPlanOperation(action),
                ToExecutionStatus(itemResult),
                mapping.PayloadHash,
                itemResult?.BatchHandle ?? state.BatchHandle,
                itemResult?.ErrorMessage));
        }

        if (states.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return Ok(new MetaCatalogSyncExecutionResponse(
            false,
            MetaCatalogSyncExecutionSummary.FromItems(responseItems),
            responseItems));
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

        var applicableProductIds = plan.Items
            .Where(IsApplicablePlanItem)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();
        var currentProducts = await _unitOfWork.Products.GetByIdsForBrandBackfillAsync(applicableProductIds);
        var currentProductsById = currentProducts.ToDictionary(p => p.Id);

        var appliedItems = new List<MetaCatalogBrandBackfillItem>();
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

            if (!currentProductsById.TryGetValue(planItem.ProductId, out var currentProduct))
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
            appliedItems.Add(new MetaCatalogBrandBackfillItem(
                currentProduct.Id,
                currentProduct.Name,
                previousBrand,
                newBrand,
                MetaCatalogBrandBackfillApplyOperations.Updated,
                currentPlanItem.Reason));
        }

        await _unitOfWork.SaveChangesAsync();

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

    private static MetaCatalogSyncExecutionItem CreateDryRunSyncItem(MetaCatalogSyncPlanItem item)
    {
        return new MetaCatalogSyncExecutionItem(
            item.ProductId,
            item.Name,
            item.Operation,
            IsExecutableSyncOperation(item)
                ? MetaCatalogSyncExecutionStatuses.Skipped
                : ToSkippedStatus(item),
            item.PayloadHash,
            null,
            item.Reason);
    }

    private static MetaCatalogSyncExecutionItem CreateNonSubmittedSyncItem(MetaCatalogSyncPlanItem item)
    {
        return new MetaCatalogSyncExecutionItem(
            item.ProductId,
            item.Name,
            item.Operation,
            ToSkippedStatus(item),
            item.PayloadHash,
            null,
            item.Reason);
    }

    private static bool IsExecutableSyncOperation(MetaCatalogSyncPlanItem item)
    {
        return item.Operation is MetaCatalogSyncPlanOperations.Create
            or MetaCatalogSyncPlanOperations.Update
            or MetaCatalogSyncPlanOperations.Delete;
    }

    private static string ToSkippedStatus(MetaCatalogSyncPlanItem item)
    {
        return string.Equals(item.Operation, MetaCatalogSyncPlanOperations.Unchanged, StringComparison.Ordinal)
            ? MetaCatalogSyncExecutionStatuses.Unchanged
            : MetaCatalogSyncExecutionStatuses.Skipped;
    }

    private static (MetaProductSyncState State, bool IsNew) GetOrCreateSyncState(
        IDictionary<Guid, MetaProductSyncState> statesByProductId,
        Guid productId,
        string retailerId,
        DateTime now)
    {
        if (statesByProductId.TryGetValue(productId, out var state))
        {
            state.RetailerId = retailerId;
            return (state, false);
        }

        state = new MetaProductSyncState
        {
            ProductId = productId,
            RetailerId = retailerId,
            CreatedAt = now
        };
        statesByProductId[productId] = state;
        return (state, true);
    }

    private static void ApplyMetaResultToState(
        MetaProductSyncState state,
        MetaCatalogMappingResult mapping,
        MetaCatalogItemResult? result,
        DateTime now)
    {
        state.RetailerId = mapping.RetailerId;
        state.LastAction = mapping.Action.ToString();
        state.LastAttemptAt = now;
        state.UpdatedAt = now;

        if (result?.Success == true)
        {
            state.Status = MetaProductSyncStatuses.Synced;
            state.LastPayloadHash = mapping.PayloadHash;
            state.PendingPayloadHash = null;
            state.LastSyncedAt = now;
            state.BatchHandle = result.BatchHandle;
            state.LastErrorCode = null;
            state.LastErrorSubcode = null;
            state.LastErrorMessage = null;
            state.RetryCount = 0;
            return;
        }

        if (result?.IsTransient == true && string.Equals(result.Status, "processing", StringComparison.OrdinalIgnoreCase))
        {
            state.Status = MetaProductSyncStatuses.Processing;
            state.PendingPayloadHash = mapping.PayloadHash;
            state.BatchHandle = result.BatchHandle;
            state.LastErrorCode = null;
            state.LastErrorSubcode = null;
            state.LastErrorMessage = result.ErrorMessage;
            return;
        }

        state.Status = MetaProductSyncStatuses.Error;
        state.PendingPayloadHash = mapping.PayloadHash;
        state.BatchHandle = result?.BatchHandle;
        state.LastErrorCode = result?.ErrorCode;
        state.LastErrorSubcode = result?.ErrorSubcode;
        state.LastErrorMessage = result?.ErrorMessage ?? "Meta Catalog did not return a result for this product.";
        state.RetryCount += 1;
    }

    private static string ToExecutionStatus(MetaCatalogItemResult? result)
    {
        if (result?.Success == true)
        {
            return MetaCatalogSyncExecutionStatuses.Synced;
        }

        if (result?.IsTransient == true && string.Equals(result.Status, "processing", StringComparison.OrdinalIgnoreCase))
        {
            return MetaCatalogSyncExecutionStatuses.Processing;
        }

        return MetaCatalogSyncExecutionStatuses.Error;
    }

    private static MetaCatalogSyncAction ParseSyncAction(string? value)
    {
        return Enum.TryParse<MetaCatalogSyncAction>(value, ignoreCase: true, out var action)
            ? action
            : MetaCatalogSyncAction.Upsert;
    }

    private static string ToPlanOperation(MetaCatalogSyncAction action)
    {
        return action == MetaCatalogSyncAction.Delete
            ? MetaCatalogSyncPlanOperations.Delete
            : MetaCatalogSyncPlanOperations.Update;
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

public static class MetaCatalogSyncExecutionStatuses
{
    public const string Synced = "SYNCED";
    public const string Processing = "PROCESSING";
    public const string Error = "ERROR";
    public const string Skipped = "SKIPPED";
    public const string Unchanged = "UNCHANGED";
}

public record MetaCatalogSyncExecutionResponse(
    bool DryRun,
    MetaCatalogSyncExecutionSummary Summary,
    IReadOnlyList<MetaCatalogSyncExecutionItem> Items);

public record MetaCatalogSyncExecutionSummary(
    int Scanned,
    int Submitted,
    int Synced,
    int Processing,
    int Failed,
    int Unchanged,
    int Skipped)
{
    public static MetaCatalogSyncExecutionSummary FromItems(
        IReadOnlyCollection<MetaCatalogSyncExecutionItem> items,
        int? submitted = null)
    {
        return new MetaCatalogSyncExecutionSummary(
            items.Count,
            submitted ?? items.Count(i => i.Status is MetaCatalogSyncExecutionStatuses.Synced
                or MetaCatalogSyncExecutionStatuses.Processing
                or MetaCatalogSyncExecutionStatuses.Error),
            CountStatus(items, MetaCatalogSyncExecutionStatuses.Synced),
            CountStatus(items, MetaCatalogSyncExecutionStatuses.Processing),
            CountStatus(items, MetaCatalogSyncExecutionStatuses.Error),
            CountStatus(items, MetaCatalogSyncExecutionStatuses.Unchanged),
            CountStatus(items, MetaCatalogSyncExecutionStatuses.Skipped));
    }

    private static int CountStatus(IEnumerable<MetaCatalogSyncExecutionItem> items, string status)
    {
        return items.Count(i => string.Equals(i.Status, status, StringComparison.Ordinal));
    }
}

public record MetaCatalogSyncExecutionItem(
    Guid ProductId,
    string Name,
    string Operation,
    string Status,
    string? PayloadHash,
    string? BatchHandle,
    string? Message);
