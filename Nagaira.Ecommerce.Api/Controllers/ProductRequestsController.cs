using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
[Route("api/product-requests")]
[AllowAnonymous]
public class ProductRequestsController : ControllerBase
{
    private static readonly HashSet<string> AllowedUrgencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "low",
        "normal",
        "high",
        "urgent"
    };
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<ProductRequestsController> _logger;

    public ProductRequestsController(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, ILogger<ProductRequestsController> logger)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ProductRequestDto>> Create(
        [FromForm] string description,
        [FromForm] string name,
        [FromForm] string phone,
        [FromForm] string urgency,
        [FromForm] string? email,
        [FromForm] string? city,
        [FromForm] string? address,
        [FromForm] string? link,
        IFormFile? image)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return BadRequest(new { message = "La descripcion es obligatoria." });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "El nombre es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            return BadRequest(new { message = "El telefono es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(urgency))
        {
            return BadRequest(new { message = "La urgencia es obligatoria." });
        }

        if (!AllowedUrgencies.Contains(urgency))
        {
            return BadRequest(new { message = "Urgencia no valida." });
        }

        string? imageUrl = null;
        string? imageName = null;

        if (image != null)
        {
            if (!image.ContentType.StartsWith("image/"))
            {
                return BadRequest(new { message = "El archivo debe ser una imagen." });
            }

            if (image.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { message = "La imagen no puede ser mayor a 10MB." });
            }

            try
            {
                using var sourceStream = image.OpenReadStream();
                imageName = image.FileName;
                imageUrl = await _cloudinaryService.UploadImageAsync(sourceStream, imageName, "product_requests");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir imagen de solicitud");
                return StatusCode(500, new { message = "Error al subir la imagen." });
            }
        }

        var request = new ProductRequest
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Phone = phone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            Description = description.Trim(),
            Urgency = urgency.Trim().ToLowerInvariant(),
            Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim(),
            ImageUrl = imageUrl,
            ImageName = imageName,
            Status = "new",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Repository<ProductRequest>().AddAsync(request);
        await _unitOfWork.SaveChangesAsync();

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
}
