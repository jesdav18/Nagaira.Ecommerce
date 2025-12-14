using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/upload")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminUploadController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<AdminUploadController> _logger;

    public AdminUploadController(ICloudinaryService cloudinaryService, ILogger<AdminUploadController> logger)
    {
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    [HttpPost("image")]
    public async Task<ActionResult<object>> UploadImage(IFormFile file, [FromQuery] string? folder = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No se proporcionó ningún archivo" });
        }

        if (!file.ContentType.StartsWith("image/"))
        {
            return BadRequest(new { message = "El archivo debe ser una imagen" });
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { message = "La imagen no puede ser mayor a 10MB" });
        }

        try
        {
            using var sourceStream = file.OpenReadStream();
            var fileName = file.FileName;
            var imageUrl = await _cloudinaryService.UploadImageAsync(sourceStream, fileName, folder);

            return Ok(new { imageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir imagen a Cloudinary");
            return StatusCode(500, new { message = "Error al subir la imagen", error = ex.Message });
        }
    }
}

