using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController, Route("api/admin/brands"), Authorize(Roles = "Admin,SuperAdmin")]
public class AdminBrandsController(IBrandService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> Get([FromQuery] string? search = null, [FromQuery] bool activeOnly = true) => Ok(await service.SearchAsync(search, activeOnly));
    [HttpPost]
    public async Task<ActionResult<BrandDto>> Create(SaveBrandDto dto) => Ok(await service.CreateAsync(dto));
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BrandDto>> Update(Guid id, SaveBrandDto dto)
    {
        try { return Ok(await service.UpdateAsync(id, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) when (ex.Message == "brand_already_exists") { return Conflict(new { message = ex.Message }); }
    }
}
