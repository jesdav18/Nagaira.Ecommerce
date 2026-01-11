using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/product-requests")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminProductRequestsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "new",
        "in_progress",
        "contacted",
        "closed"
    };

    public AdminProductRequestsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductRequestDto>>> GetAll()
    {
        var requests = await _unitOfWork.Repository<ProductRequest>().FindAsync(r => !r.IsDeleted);
        var result = requests
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ProductRequestDto(
                r.Id,
                r.Name,
                r.Phone,
                r.Email,
                r.City,
                r.Address,
                r.Description,
                r.Urgency,
                r.Link,
                r.ImageUrl,
                r.ImageName,
                r.Status,
                r.CreatedAt));

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductRequestDto>> GetById(Guid id)
    {
        var request = await _unitOfWork.Repository<ProductRequest>().GetByIdAsync(id);
        if (request == null || request.IsDeleted)
        {
            return NotFound();
        }

        return Ok(new ProductRequestDto(
            request.Id,
            request.Name,
            request.Phone,
            request.Email,
            request.City,
            request.Address,
            request.Description,
            request.Urgency,
            request.Link,
            request.ImageUrl,
            request.ImageName,
            request.Status,
            request.CreatedAt
        ));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateProductRequestStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new { message = "El estado es obligatorio." });
        }

        if (!AllowedStatuses.Contains(dto.Status))
        {
            return BadRequest(new { message = "Estado no valido." });
        }

        var request = await _unitOfWork.Repository<ProductRequest>().GetByIdAsync(id);
        if (request == null || request.IsDeleted)
        {
            return NotFound();
        }

        request.Status = dto.Status.Trim().ToLowerInvariant();
        request.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<ProductRequest>().UpdateAsync(request);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}

public record UpdateProductRequestStatusDto(string Status);
