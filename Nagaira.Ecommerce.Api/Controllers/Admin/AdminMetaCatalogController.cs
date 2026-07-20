using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nagaira.Ecommerce.Application.MetaCatalog;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/meta-catalog")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminMetaCatalogController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly MetaCatalogOptions _options;

    public AdminMetaCatalogController(IUnitOfWork unitOfWork, IOptions<MetaCatalogOptions> options)
    {
        _unitOfWork = unitOfWork;
        _options = options.Value;
    }

    [HttpPost("products/{productId:guid}/test-sync")]
    public async Task<ActionResult<MetaCatalogTestSyncResponse>> TestSync(
        Guid productId,
        [FromQuery] bool dryRun = true)
    {
        if (!dryRun)
        {
            return BadRequest(new { message = "Only dryRun=true is supported for test sync." });
        }

        var product = await _unitOfWork.Products.GetByIdIncludingDeletedAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        var result = MetaCatalogProductMapper.Map(product, _options);
        return Ok(new MetaCatalogTestSyncResponse(
            result.RetailerId,
            result.Action.ToString(),
            result.PayloadHash,
            result.Item));
    }
}

public record MetaCatalogTestSyncResponse(
    string RetailerId,
    string Action,
    string PayloadHash,
    MetaCatalogProduct? Payload);
