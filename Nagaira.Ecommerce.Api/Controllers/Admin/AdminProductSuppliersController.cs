using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Api.Helpers;
using System.Text.Json;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/product-suppliers")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminProductSuppliersController : ControllerBase
{
    private readonly IProductSupplierService _productSupplierService;
    private readonly IAuditService _auditService;

    public AdminProductSuppliersController(IProductSupplierService productSupplierService, IAuditService auditService)
    {
        _productSupplierService = productSupplierService;
        _auditService = auditService;
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<ActionResult<IEnumerable<ProductSupplierDto>>> GetByProduct(Guid productId)
    {
        var productSuppliers = await _productSupplierService.GetByProductIdAsync(productId);
        return Ok(productSuppliers);
    }

    [HttpGet("supplier/{supplierId:guid}")]
    public async Task<ActionResult<IEnumerable<ProductSupplierDto>>> GetBySupplier(Guid supplierId)
    {
        var productSuppliers = await _productSupplierService.GetBySupplierIdAsync(supplierId);
        return Ok(productSuppliers);
    }

    [HttpGet("product/{productId:guid}/primary")]
    public async Task<ActionResult<ProductSupplierDto>> GetPrimarySupplier(Guid productId)
    {
        var productSupplier = await _productSupplierService.GetPrimarySupplierByProductIdAsync(productId);
        if (productSupplier == null) return NotFound();
        return Ok(productSupplier);
    }

    [HttpPost]
    public async Task<ActionResult<ProductSupplierDto>> Create([FromBody] CreateProductSupplierDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var productSupplier = await _productSupplierService.CreateProductSupplierAsync(dto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "CREATE",
                "ProductSupplier",
                productSupplier.Id,
                null,
                JsonSerializer.Serialize(dto)
            );

            return CreatedAtAction(nameof(GetByProduct), new { productId = dto.ProductId }, productSupplier);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductSupplierDto dto, [FromQuery] string? changeReason = null)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var userId = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Guid? userIdGuid = Guid.TryParse(userId, out var parsed) ? parsed : null;

            await _productSupplierService.UpdateProductSupplierAsync(dto, userIdGuid, changeReason);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "UPDATE",
                "ProductSupplier",
                id,
                null,
                JsonSerializer.Serialize(dto)
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _productSupplierService.DeleteProductSupplierAsync(id);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DELETE",
                "ProductSupplier",
                id,
                null,
                null
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("product/{productId:guid}/supplier/{supplierId:guid}/set-primary")]
    public async Task<IActionResult> SetAsPrimary(Guid productId, Guid supplierId)
    {
        try
        {
            await _productSupplierService.SetAsPrimaryAsync(productId, supplierId);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "SET_PRIMARY",
                "ProductSupplier",
                productId,
                null,
                JsonSerializer.Serialize(new { productId, supplierId })
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("product/{productId:guid}/best-cost")]
    public async Task<ActionResult<decimal?>> GetBestCost(Guid productId)
    {
        var cost = await _productSupplierService.GetBestSupplierCostAsync(productId);
        return Ok(new { cost });
    }

    [HttpGet("product-supplier/{productSupplierId:guid}/cost-history")]
    public async Task<ActionResult<IEnumerable<SupplierCostHistoryDto>>> GetCostHistory(Guid productSupplierId)
    {
        var history = await _productSupplierService.GetCostHistoryAsync(productSupplierId);
        return Ok(history);
    }
}

