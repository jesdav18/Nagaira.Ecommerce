using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using System.Security.Claims;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminInventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public AdminInventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("products/{productId:guid}")]
    public async Task<ActionResult<InventoryBalanceDto>> GetProductBalance(Guid productId)
    {
        var balance = await _inventoryService.GetProductBalanceAsync(productId);
        return Ok(balance);
    }

    [HttpGet("products/{productId:guid}/movements")]
    public async Task<ActionResult<IEnumerable<InventoryMovementDto>>> GetProductMovements(Guid productId)
    {
        var movements = await _inventoryService.GetMovementsByProductAsync(productId);
        return Ok(movements);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<InventoryBalanceDto>>> GetLowStock([FromQuery] int threshold = 10)
    {
        var balances = await _inventoryService.GetLowStockProductsAsync(threshold);
        return Ok(balances);
    }

    [HttpGet("products")]
    public async Task<ActionResult<IEnumerable<InventoryBalanceDto>>> GetAllProducts()
    {
        var balances = await _inventoryService.GetAllProductBalancesAsync();
        return Ok(balances);
    }

    [HttpPost("movements")]
    public async Task<ActionResult<InventoryMovementDto>> CreateMovement([FromBody] CreateInventoryMovementDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var movement = await _inventoryService.CreateMovementAsync(dto, userId);
            return CreatedAtAction(nameof(GetProductMovements), new { productId = dto.ProductId }, movement);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("movements/reference/{referenceType}/{referenceId:guid}")]
    public async Task<ActionResult<IEnumerable<InventoryMovementDto>>> GetMovementsByReference(
        string referenceType, 
        Guid referenceId)
    {
        var movements = await _inventoryService.GetMovementsByReferenceAsync(referenceType, referenceId);
        return Ok(movements);
    }

    [HttpGet("movement-types")]
    public async Task<ActionResult<IEnumerable<MovementTypeDto>>> GetMovementTypes()
    {
        var types = await _inventoryService.GetMovementTypesAsync();
        return Ok(types);
    }
}

