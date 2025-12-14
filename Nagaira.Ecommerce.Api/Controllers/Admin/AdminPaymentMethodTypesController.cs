using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using System.Text.Json;
using Nagaira.Ecommerce.Api.Helpers;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/payment-method-types")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminPaymentMethodTypesController : ControllerBase
{
    private readonly IPaymentMethodTypeService _paymentMethodTypeService;
    private readonly IAuditService _auditService;

    public AdminPaymentMethodTypesController(IPaymentMethodTypeService paymentMethodTypeService, IAuditService auditService)
    {
        _paymentMethodTypeService = paymentMethodTypeService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentMethodTypeDto>>> GetAll()
    {
        var types = await _paymentMethodTypeService.GetAllPaymentMethodTypesAsync();
        return Ok(types);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentMethodTypeDto>> GetById(Guid id)
    {
        var type = await _paymentMethodTypeService.GetPaymentMethodTypeByIdAsync(id);
        if (type == null) return NotFound();
        return Ok(type);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentMethodTypeDto>> Create([FromBody] CreatePaymentMethodTypeDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var type = await _paymentMethodTypeService.CreatePaymentMethodTypeAsync(dto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "CREATE",
                "PaymentMethodType",
                type.Id,
                null,
                JsonSerializer.Serialize(dto)
            );

            return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentMethodTypeDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var existing = await _paymentMethodTypeService.GetPaymentMethodTypeByIdAsync(id);
            var oldValues = existing != null ? JsonSerializer.Serialize(existing) : null;

            await _paymentMethodTypeService.UpdatePaymentMethodTypeAsync(dto);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "UPDATE",
                "PaymentMethodType",
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
            var existing = await _paymentMethodTypeService.GetPaymentMethodTypeByIdAsync(id);
            var oldValues = existing != null ? JsonSerializer.Serialize(existing) : null;

            await _paymentMethodTypeService.DeletePaymentMethodTypeAsync(id);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DELETE",
                "PaymentMethodType",
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
            await _paymentMethodTypeService.ActivatePaymentMethodTypeAsync(id);
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
            await _paymentMethodTypeService.DeactivatePaymentMethodTypeAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

