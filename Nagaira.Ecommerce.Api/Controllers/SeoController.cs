using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
[Route("api/seo")]
public class SeoController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SeoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("resolve")]
    public async Task<ActionResult<SlugResolveDto>> Resolve([FromQuery] string type, [FromQuery] string slug)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(slug))
        {
            return BadRequest(new { message = "Type y slug son obligatorios." });
        }

        var entityType = type.Trim().ToLowerInvariant();
        if (entityType != "product" && entityType != "category")
        {
            return BadRequest(new { message = "Tipo no valido." });
        }

        if (entityType == "product")
        {
            var current = await _unitOfWork.Products.GetBySlugAsync(slug);
            if (current != null)
            {
                return Ok(new SlugResolveDto(current.Slug));
            }
        }
        else
        {
            var current = await _unitOfWork.Categories.GetBySlugAsync(slug);
            if (current != null)
            {
                return Ok(new SlugResolveDto(current.Slug));
            }
        }

        var history = await _unitOfWork.Repository<SlugHistory>()
            .FindAsync(h => h.EntityType == entityType && h.Slug == slug && !h.IsDeleted);
        var entry = history.FirstOrDefault();
        if (entry == null)
        {
            return NotFound();
        }

        if (entityType == "product")
        {
            var product = await _unitOfWork.Products.GetByIdAsync(entry.EntityId);
            if (product == null || product.IsDeleted || !product.IsActive)
            {
                return NotFound();
            }
            return Ok(new SlugResolveDto(product.Slug));
        }

        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(entry.EntityId);
        if (category == null || category.IsDeleted || !category.IsActive)
        {
            return NotFound();
        }
        return Ok(new SlugResolveDto(category.Slug));
    }
}

public record SlugResolveDto(string Slug);
