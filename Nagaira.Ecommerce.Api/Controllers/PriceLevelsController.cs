using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
[Route("api/admin/price-levels")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class PriceLevelsController : ControllerBase
{
    private readonly IPriceLevelService _priceLevelService;

    public PriceLevelsController(IPriceLevelService priceLevelService)
    {
        _priceLevelService = priceLevelService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PriceLevelDto>>> GetAll()
    {
        var levels = await _priceLevelService.GetAllPriceLevelsAsync();
        return Ok(levels);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PriceLevelDto>> GetById(Guid id)
    {
        var level = await _priceLevelService.GetPriceLevelByIdAsync(id);
        if (level == null) return NotFound();
        return Ok(level);
    }

    [HttpPost]
    public async Task<ActionResult<PriceLevelDto>> Create([FromBody] CreatePriceLevelDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var level = await _priceLevelService.CreatePriceLevelAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = level.Id }, level);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePriceLevelDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            await _priceLevelService.UpdatePriceLevelAsync(dto);
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
            await _priceLevelService.DeletePriceLevelAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

