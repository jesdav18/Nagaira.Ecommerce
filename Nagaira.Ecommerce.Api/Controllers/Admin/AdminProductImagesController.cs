using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/product-images")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminProductImagesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminProductImagesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<ActionResult<IEnumerable<ProductImageDto>>> GetByProduct(Guid productId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null) return NotFound();
        
        var images = product.Images
            .Where(i => !i.IsDeleted)
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.AltText, i.IsPrimary, i.DisplayOrder))
            .ToList();
        
        return Ok(images);
    }

    [HttpPost]
    public async Task<ActionResult<ProductImageDto>> Create([FromBody] CreateProductImageDto dto)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
        if (product == null) return BadRequest("Product not found");

        var existing = product.Images.FirstOrDefault(i => !i.IsDeleted && i.ImageUrl == dto.ImageUrl);
        if (existing != null)
        {
            return Ok(new ProductImageDto(existing.Id, existing.ImageUrl, existing.AltText, existing.IsPrimary, existing.DisplayOrder));
        }

        if (dto.IsPrimary)
        {
            var existingPrimary = product.Images.FirstOrDefault(i => i.IsPrimary && !i.IsDeleted);
            if (existingPrimary != null)
            {
                existingPrimary.IsPrimary = false;
            }
        }

        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            ImageUrl = dto.ImageUrl,
            AltText = dto.AltText,
            IsPrimary = dto.IsPrimary,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<ProductImage>().AddAsync(image);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByProduct), new { productId = image.ProductId }, 
            new ProductImageDto(image.Id, image.ImageUrl, image.AltText, image.IsPrimary, image.DisplayOrder));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductImageDto dto)
    {
        var image = await _unitOfWork.Repository<ProductImage>().GetByIdAsync(id);
        if (image == null || image.IsDeleted) return NotFound();

        if (dto.ImageUrl != null) image.ImageUrl = dto.ImageUrl;
        if (dto.AltText != null) image.AltText = dto.AltText;
        if (dto.IsPrimary.HasValue)
        {
            if (dto.IsPrimary.Value)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(image.ProductId);
                if (product != null)
                {
                    var existingPrimary = product.Images.FirstOrDefault(i => i.IsPrimary && !i.IsDeleted && i.Id != id);
                    if (existingPrimary != null)
                    {
                        existingPrimary.IsPrimary = false;
                    }
                }
            }
            image.IsPrimary = dto.IsPrimary.Value;
        }
        if (dto.DisplayOrder.HasValue) image.DisplayOrder = dto.DisplayOrder.Value;

        await _unitOfWork.Repository<ProductImage>().UpdateAsync(image);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _unitOfWork.Repository<ProductImage>().DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }
}

