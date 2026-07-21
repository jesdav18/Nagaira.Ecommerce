using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Application.MetaCatalog;
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
                null));
        }
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
    string? DiagnosticResponseBody)
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
        string? diagnosticResponseBody = null)
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
            diagnosticResponseBody);
    }
}
