using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using System.Security.Claims;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/offers")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminOffersController : ControllerBase
{
    private readonly IOfferService _offerService;

    public AdminOffersController(IOfferService offerService)
    {
        _offerService = offerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OfferDto>>> GetAll()
    {
        var offers = await _offerService.GetAllOffersAsync();
        return Ok(offers);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<OfferDto>>> GetActive()
    {
        var offers = await _offerService.GetActiveOffersAsync();
        return Ok(offers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OfferDto>> GetById(Guid id)
    {
        var offer = await _offerService.GetOfferByIdAsync(id);
        if (offer == null) return NotFound();
        return Ok(offer);
    }

    [HttpPost]
    public async Task<ActionResult<OfferDto>> Create([FromBody] CreateOfferDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var offer = await _offerService.CreateOfferAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = offer.Id }, offer);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOfferDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            await _offerService.UpdateOfferAsync(dto);
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
            await _offerService.DeleteOfferAsync(id);
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
            await _offerService.ActivateOfferAsync(id);
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
            await _offerService.DeactivateOfferAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}/applications")]
    public async Task<ActionResult<IEnumerable<OfferApplicationDto>>> GetApplications(Guid id)
    {
        var applications = await _offerService.GetOfferApplicationsAsync(id);
        return Ok(applications);
    }
}

