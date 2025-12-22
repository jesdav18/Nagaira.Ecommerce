using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Api.Helpers;
using System.Text.Json;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/suppliers")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminSuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly IAuditService _auditService;

    public AdminSuppliersController(ISupplierService supplierService, IAuditService auditService)
    {
        _supplierService = supplierService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetAll()
    {
        var suppliers = await _supplierService.GetAllSuppliersAsync();
        return Ok(suppliers);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetActive()
    {
        var suppliers = await _supplierService.GetActiveSuppliersAsync();
        return Ok(suppliers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> GetById(Guid id)
    {
        var supplier = await _supplierService.GetSupplierByIdAsync(id);
        if (supplier == null) return NotFound();
        return Ok(supplier);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var supplier = await _supplierService.CreateSupplierAsync(dto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "CREATE",
                "Supplier",
                supplier.Id,
                null,
                JsonSerializer.Serialize(dto)
            );

            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var existing = await _supplierService.GetSupplierByIdAsync(id);
            var oldValues = existing != null ? JsonSerializer.Serialize(existing) : null;

            await _supplierService.UpdateSupplierAsync(dto);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "UPDATE",
                "Supplier",
                id,
                oldValues,
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
            var existing = await _supplierService.GetSupplierByIdAsync(id);
            var oldValues = existing != null ? JsonSerializer.Serialize(existing) : null;

            await _supplierService.DeleteSupplierAsync(id);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DELETE",
                "Supplier",
                id,
                oldValues,
                null
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        try
        {
            await _supplierService.ActivateSupplierAsync(id);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "ACTIVATE",
                "Supplier",
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

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            await _supplierService.DeactivateSupplierAsync(id);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DEACTIVATE",
                "Supplier",
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
}

