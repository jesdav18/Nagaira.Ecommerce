using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/banners")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminBannersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminBannersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BannerDto>>> GetAll()
    {
        var banners = await _unitOfWork.Repository<Banner>().FindAsync(b => !b.IsDeleted);
        var result = banners
            .OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.CreatedAt)
            .Select(MapToDto);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BannerDto>> GetById(Guid id)
    {
        var banner = await _unitOfWork.Repository<Banner>().GetByIdAsync(id);
        if (banner == null || banner.IsDeleted) return NotFound();
        return Ok(MapToDto(banner));
    }

    [HttpPost]
    public async Task<ActionResult<BannerDto>> Create([FromBody] CreateBannerDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.ImageUrl))
        {
            return BadRequest(new { message = "Titulo e imagen son obligatorios." });
        }

        var banner = new Banner
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Subtitle = string.IsNullOrWhiteSpace(dto.Subtitle) ? null : dto.Subtitle.Trim(),
            ImageUrl = dto.ImageUrl.Trim(),
            LinkUrl = string.IsNullOrWhiteSpace(dto.LinkUrl) ? null : dto.LinkUrl.Trim(),
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Repository<Banner>().AddAsync(banner);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = banner.Id }, MapToDto(banner));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBannerDto dto)
    {
        var banner = await _unitOfWork.Repository<Banner>().GetByIdAsync(id);
        if (banner == null || banner.IsDeleted) return NotFound();

        if (dto.Title != null) banner.Title = dto.Title.Trim();
        if (dto.Subtitle != null) banner.Subtitle = string.IsNullOrWhiteSpace(dto.Subtitle) ? null : dto.Subtitle.Trim();
        if (dto.ImageUrl != null) banner.ImageUrl = dto.ImageUrl.Trim();
        if (dto.LinkUrl != null) banner.LinkUrl = string.IsNullOrWhiteSpace(dto.LinkUrl) ? null : dto.LinkUrl.Trim();
        if (dto.DisplayOrder.HasValue) banner.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.IsActive.HasValue) banner.IsActive = dto.IsActive.Value;
        banner.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Banner>().UpdateAsync(banner);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var banner = await _unitOfWork.Repository<Banner>().GetByIdAsync(id);
        if (banner == null || banner.IsDeleted) return NotFound();

        banner.IsDeleted = true;
        banner.IsActive = false;
        banner.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Banner>().UpdateAsync(banner);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    private static BannerDto MapToDto(Banner banner)
    {
        return new BannerDto(
            banner.Id,
            banner.Title,
            banner.Subtitle,
            banner.ImageUrl,
            banner.LinkUrl,
            banner.DisplayOrder,
            banner.IsActive,
            banner.CreatedAt
        );
    }
}
