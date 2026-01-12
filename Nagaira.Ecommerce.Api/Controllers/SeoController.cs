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
    private readonly IConfiguration _configuration;

    public SeoController(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
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

    [HttpGet("redirect/{type}/{slug}")]
    public async Task<IActionResult> RedirectLegacy(string type, string slug)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(slug))
        {
            return BadRequest(new { message = "Type y slug son obligatorios." });
        }

        var entityType = type.Trim().ToLowerInvariant();
        if (entityType != "product" && entityType != "category" && entityType != "p" && entityType != "c")
        {
            return BadRequest(new { message = "Tipo no valido." });
        }

        var resolvedType = entityType switch
        {
            "p" => "product",
            "c" => "category",
            _ => entityType
        };

        var result = await ResolveInternal(resolvedType, slug);
        if (result == null)
        {
            return NotFound();
        }

        if (string.Equals(result, slug, StringComparison.OrdinalIgnoreCase))
        {
            return NoContent();
        }

        var baseUrl = ResolveBaseUrl();
        var path = resolvedType == "product" ? $"/p/{result}" : $"/c/{result}";
        return RedirectPermanent($"{baseUrl}{path}");
    }

    private async Task<string?> ResolveInternal(string entityType, string slug)
    {
        if (entityType == "product")
        {
            var current = await _unitOfWork.Products.GetBySlugAsync(slug);
            if (current != null)
            {
                return current.Slug;
            }
        }
        else
        {
            var current = await _unitOfWork.Categories.GetBySlugAsync(slug);
            if (current != null)
            {
                return current.Slug;
            }
        }

        var history = await _unitOfWork.Repository<SlugHistory>()
            .FindAsync(h => h.EntityType == entityType && h.Slug == slug && !h.IsDeleted);
        var entry = history.FirstOrDefault();
        if (entry == null)
        {
            return null;
        }

        if (entityType == "product")
        {
            var product = await _unitOfWork.Products.GetByIdAsync(entry.EntityId);
            return product != null && !product.IsDeleted && product.IsActive ? product.Slug : null;
        }

        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(entry.EntityId);
        return category != null && !category.IsDeleted && category.IsActive ? category.Slug : null;
    }

    private string ResolveBaseUrl()
    {
        var configBase = _configuration["Seo:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configBase))
        {
            return configBase.TrimEnd('/');
        }

        return $"{Request.Scheme}://{Request.Host}/ecommerce";
    }
}

public record SlugResolveDto(string Slug);
