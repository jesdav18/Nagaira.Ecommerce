using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
[Route("api/banners")]
[AllowAnonymous]
public class BannersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public BannersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BannerDto>>> GetActive()
    {
        var banners = await _unitOfWork.Repository<Banner>()
            .FindAsync(b => b.IsActive && !b.IsDeleted);

        var result = banners
            .OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.CreatedAt)
            .Select(MapToDto);

        return Ok(result);
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
