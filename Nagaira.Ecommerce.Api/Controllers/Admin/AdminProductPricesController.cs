using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/product-prices")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminProductPricesController : ControllerBase
{
    private readonly IProductPriceService _productPriceService;

    public AdminProductPricesController(IProductPriceService productPriceService)
    {
        _productPriceService = productPriceService;
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<ActionResult<IEnumerable<ProductPriceDto>>> GetByProduct(Guid productId)
    {
        var prices = await _productPriceService.GetPricesByProductAsync(productId);
        return Ok(prices);
    }

    [HttpGet("level/{levelId:guid}")]
    public async Task<ActionResult<IEnumerable<ProductPriceDto>>> GetByLevel(Guid levelId)
    {
        var prices = await _productPriceService.GetPricesByLevelAsync(levelId);
        return Ok(prices);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductPriceDto>> GetById(Guid id)
    {
        var price = await _productPriceService.GetPriceByIdAsync(id);
        if (price == null) return NotFound();
        return Ok(price);
    }

    [HttpPost]
    public async Task<ActionResult<ProductPriceDto>> Create([FromBody] CreateProductPriceDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var price = await _productPriceService.CreatePriceAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = price.Id }, price);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductPriceDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            await _productPriceService.UpdatePriceAsync(dto);
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
            await _productPriceService.DeletePriceAsync(id);
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
            await _productPriceService.ActivatePriceAsync(id);
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
            await _productPriceService.DeactivatePriceAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

