using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using System.Text.Json;
using Nagaira.Ecommerce.Api.Helpers;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/payment-methods")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminPaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethodService _paymentMethodService;
    private readonly IAuditService _auditService;

    public AdminPaymentMethodsController(IPaymentMethodService paymentMethodService, IAuditService auditService)
    {
        _paymentMethodService = paymentMethodService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentMethodDto>>> GetAll()
    {
        var paymentMethods = await _paymentMethodService.GetAllPaymentMethodsAsync();
        return Ok(paymentMethods);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentMethodDto>> GetById(Guid id)
    {
        var paymentMethod = await _paymentMethodService.GetPaymentMethodByIdAsync(id);
        if (paymentMethod == null) return NotFound();
        return Ok(paymentMethod);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentMethodDto>> Create([FromBody] CreatePaymentMethodDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var paymentMethod = await _paymentMethodService.CreatePaymentMethodAsync(dto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "CREATE",
                "PaymentMethod",
                paymentMethod.Id,
                null,
                JsonSerializer.Serialize(dto)
            );

            return CreatedAtAction(nameof(GetById), new { id = paymentMethod.Id }, paymentMethod);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentMethodDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var existing = await _paymentMethodService.GetPaymentMethodByIdAsync(id);
            var oldValues = existing != null ? JsonSerializer.Serialize(existing) : null;

            await _paymentMethodService.UpdatePaymentMethodAsync(dto);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "UPDATE",
                "PaymentMethod",
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
            var existing = await _paymentMethodService.GetPaymentMethodByIdAsync(id);
            var oldValues = existing != null ? JsonSerializer.Serialize(existing) : null;

            await _paymentMethodService.DeletePaymentMethodAsync(id);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DELETE",
                "PaymentMethod",
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
            await _paymentMethodService.ActivatePaymentMethodAsync(id);
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
            await _paymentMethodService.DeactivatePaymentMethodAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<PaymentMethodTypeSimpleDto>>> GetPaymentMethodTypes()
    {
        var types = await _paymentMethodService.GetPaymentMethodTypesAsync();
        return Ok(types);
    }
}

