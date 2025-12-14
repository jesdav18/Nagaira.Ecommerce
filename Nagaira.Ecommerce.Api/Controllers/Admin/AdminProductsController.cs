using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using System.Security.Claims;
using System.Text.Json;
using Nagaira.Ecommerce.Api.Helpers;
using Npgsql;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IProductPriceService _productPriceService;
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;

    public AdminProductsController(
        IProductService productService,
        IProductPriceService productPriceService,
        IInventoryService inventoryService,
        IAuditService auditService)
    {
        _productService = productService;
        _productPriceService = productPriceService;
        _inventoryService = inventoryService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await _productService.GetAllProductsForAdminAsync();
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await _productService.GetProductByIdForAdminAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var product = await _productService.CreateProductAsync(dto);
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "CREATE",
                "Product",
                product.Id,
                null,
                JsonSerializer.Serialize(dto)
            );
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (DbUpdateException dbEx) when (dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Error de clave duplicada (SKU duplicado)
            return BadRequest(new { message = "El SKU ya existe. Por favor, use un SKU diferente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message, innerException = ex.InnerException?.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var existingProduct = await _productService.GetProductByIdForAdminAsync(id);
            var oldValues = existingProduct != null ? JsonSerializer.Serialize(existingProduct) : null;
            
            await _productService.UpdateProductAsync(dto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "UPDATE",
                "Product",
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
            var existingProduct = await _productService.GetProductByIdForAdminAsync(id);
            var oldValues = existingProduct != null ? JsonSerializer.Serialize(existingProduct) : null;
            
            await _productService.DeleteProductAsync(id);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DELETE",
                "Product",
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
            var product = await _productService.GetProductByIdForAdminAsync(id);
            if (product == null) return NotFound();

            var updateDto = new UpdateProductDto(
                product.Id,
                product.Name,
                product.Description,
                product.Cost,
                true
            );
            await _productService.UpdateProductAsync(updateDto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "ACTIVATE",
                "Product",
                id
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
            var product = await _productService.GetProductByIdForAdminAsync(id);
            if (product == null) return NotFound();

            var updateDto = new UpdateProductDto(
                product.Id,
                product.Name,
                product.Description,
                product.Cost,
                false
            );
            await _productService.UpdateProductAsync(updateDto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DEACTIVATE",
                "Product",
                id
            );
            
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}/prices")]
    public async Task<ActionResult<IEnumerable<ProductPriceDto>>> GetPrices(Guid id)
    {
        var prices = await _productPriceService.GetPricesByProductAsync(id);
        return Ok(prices);
    }

    [HttpGet("{id:guid}/inventory")]
    public async Task<ActionResult<InventoryBalanceDto>> GetInventory(Guid id)
    {
        var balance = await _inventoryService.GetProductBalanceAsync(id);
        return Ok(balance);
    }

    [HttpGet("{id:guid}/movements")]
    public async Task<ActionResult<IEnumerable<InventoryMovementDto>>> GetMovements(Guid id)
    {
        var movements = await _inventoryService.GetMovementsByProductAsync(id);
        return Ok(movements);
    }
}

