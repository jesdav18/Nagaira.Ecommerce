using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orderService,
        ICloudinaryService cloudinaryService,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrders()
    {
        var userId = GetUserId();
        var orders = await _orderService.GetUserOrdersAsync(userId);
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        
        if (!isAdmin && order.UserId != userId)
            return Forbid();
        
        return Ok(order);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        try
        {
            var userId = GetOptionalUserId();
            var order = await _orderService.CreateOrderAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("payment-proof-image")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> UploadPaymentProofImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No se proporciono ningun archivo" });
        }

        if (!file.ContentType.StartsWith("image/"))
        {
            return BadRequest(new { message = "El archivo debe ser una imagen" });
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { message = "La imagen no puede ser mayor a 10MB" });
        }

        try
        {
            using var sourceStream = file.OpenReadStream();
            var imageUrl = await _cloudinaryService.UploadImageAsync(sourceStream, file.FileName, "payment-proofs");
            return Ok(new { imageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir comprobante de pago");
            return StatusCode(500, new { message = "Error al subir el comprobante", error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/payment-proof")]
    [AllowAnonymous]
    public async Task<ActionResult<OrderDto>> UpdatePaymentProof(Guid id, [FromBody] UpdatePaymentProofDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var order = await _orderService.UpdatePaymentProofAsync(id, dto);
            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        try
        {
            await _orderService.UpdateOrderStatusAsync(id, dto.Status);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    private Guid? GetOptionalUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public record UpdateOrderStatusDto(string Status);
