using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using System.Security.Claims;
using System.Text.Json;
using Nagaira.Ecommerce.Api.Helpers;
using Npgsql;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IProductPriceService _productPriceService;
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public AdminProductsController(
        IProductService productService,
        IProductPriceService productPriceService,
        IInventoryService inventoryService,
        IAuditService auditService,
        IUnitOfWork unitOfWork)
    {
        _productService = productService;
        _productPriceService = productPriceService;
        _inventoryService = inventoryService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await _productService.GetAllProductsForAdminAsync();
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await _productService.GetProductByIdForAdminAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var product = await _productService.CreateProductAsync(dto);
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "CREATE",
                "Product",
                product.Id,
                null,
                JsonSerializer.Serialize(dto)
            );
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (DbUpdateException dbEx) when (dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Error de clave duplicada (SKU duplicado)
            return BadRequest(new { message = "El SKU ya existe. Por favor, use un SKU diferente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message, innerException = ex.InnerException?.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var existingProduct = await _productService.GetProductByIdForAdminAsync(id);
            var oldValues = existingProduct != null ? JsonSerializer.Serialize(existingProduct) : null;
            
            await _productService.UpdateProductAsync(dto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "UPDATE",
                "Product",
                id,
                oldValues,
                JsonSerializer.Serialize(dto)
            );
            
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
            var existingProduct = await _productService.GetProductByIdForAdminAsync(id);
            var oldValues = existingProduct != null ? JsonSerializer.Serialize(existingProduct) : null;
            
            await _productService.DeleteProductAsync(id);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DELETE",
                "Product",
                id,
                oldValues,
                null
            );
            
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
            var product = await _productService.GetProductByIdForAdminAsync(id);
            if (product == null) return NotFound();

            var updateDto = new UpdateProductDto(
                product.Id,
                product.Name,
                product.Description,
                product.Cost,
                true,
                product.HasVirtualStock,
                product.IsFeatured
            );
            await _productService.UpdateProductAsync(updateDto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "ACTIVATE",
                "Product",
                id
            );
            
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
            var product = await _productService.GetProductByIdForAdminAsync(id);
            if (product == null) return NotFound();

            var updateDto = new UpdateProductDto(
                product.Id,
                product.Name,
                product.Description,
                product.Cost,
                false,
                product.HasVirtualStock,
                product.IsFeatured
            );
            await _productService.UpdateProductAsync(updateDto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DEACTIVATE",
                "Product",
                id
            );
            
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}/prices")]
    public async Task<ActionResult<IEnumerable<ProductPriceDto>>> GetPrices(Guid id)
    {
        var prices = await _productPriceService.GetPricesByProductAsync(id);
        return Ok(prices);
    }

    [HttpGet("{id:guid}/inventory")]
    public async Task<ActionResult<InventoryBalanceDto>> GetInventory(Guid id)
    {
        var balance = await _inventoryService.GetProductBalanceAsync(id);
        return Ok(balance);
    }

    [HttpGet("{id:guid}/movements")]
    public async Task<ActionResult<IEnumerable<InventoryMovementDto>>> GetMovements(Guid id)
    {
        var movements = await _inventoryService.GetMovementsByProductAsync(id);
        return Ok(movements);
    }

    [HttpPut("{id:guid}/assets")]
    public async Task<IActionResult> UpsertAssets(Guid id, [FromBody] UpsertProductAssetsDto dto)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null) return NotFound();

        var strategy = _unitOfWork.GetExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingPrices = product.Prices.Where(p => !p.IsDeleted).ToList();
                var existingImages = product.Images.Where(i => !i.IsDeleted).ToList();

                var incomingPrices = (dto.Prices ?? new List<CreateProductPriceDto>())
                    .GroupBy(p => p.PriceLevelId)
                    .Select(g => g.Last())
                    .ToList();
                var incomingImages = (dto.Images ?? new List<CreateProductImageDto>())
                    .GroupBy(i => i.ImageUrl)
                    .Select(g => g.Last())
                    .ToList();

                if (dto.Prices != null && dto.Prices.Count > 0)
                {
                    foreach (var priceDto in incomingPrices)
                    {
                        var level = await _unitOfWork.PriceLevels.GetByIdAsync(priceDto.PriceLevelId);
                        if (level == null || level.IsDeleted) continue;

                        var existing = existingPrices
                            .FirstOrDefault(p => p.PriceLevelId == priceDto.PriceLevelId);
                        if (existing != null)
                        {
                            if (existing.Price != priceDto.Price ||
                                existing.PriceWithoutTax != priceDto.PriceWithoutTax ||
                                existing.MinQuantity != priceDto.MinQuantity ||
                                !existing.IsActive)
                            {
                                existing.Price = priceDto.Price;
                                existing.PriceWithoutTax = priceDto.PriceWithoutTax;
                                existing.MinQuantity = priceDto.MinQuantity;
                                existing.IsActive = true;
                                existing.UpdatedAt = DateTime.UtcNow;
                                await _unitOfWork.ProductPrices.UpdateAsync(existing);
                            }
                        }
                        else
                        {
                            var productPrice = new ProductPrice
                            {
                                Id = Guid.NewGuid(),
                                ProductId = id,
                                PriceLevelId = priceDto.PriceLevelId,
                                Price = priceDto.Price,
                                PriceWithoutTax = priceDto.PriceWithoutTax,
                                MinQuantity = priceDto.MinQuantity,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _unitOfWork.ProductPrices.AddAsync(productPrice);
                        }
                    }
                }

                if (dto.Images != null && dto.Images.Count > 0)
                {
                    var primaryUrl = incomingImages.FirstOrDefault(i => i.IsPrimary)?.ImageUrl;

                    foreach (var imageDto in incomingImages)
                    {
                        var existing = existingImages
                            .FirstOrDefault(i => i.ImageUrl == imageDto.ImageUrl);

                        var shouldBePrimary = primaryUrl != null && imageDto.ImageUrl == primaryUrl;
                        if (existing != null)
                        {
                            if (existing.AltText != imageDto.AltText ||
                                existing.DisplayOrder != imageDto.DisplayOrder ||
                                existing.IsPrimary != shouldBePrimary)
                            {
                                existing.AltText = imageDto.AltText;
                                existing.DisplayOrder = imageDto.DisplayOrder;
                                existing.IsPrimary = shouldBePrimary;
                                await _unitOfWork.Repository<ProductImage>().UpdateAsync(existing);
                            }
                            continue;
                        }

                        var image = new ProductImage
                        {
                            Id = Guid.NewGuid(),
                            ProductId = id,
                            ImageUrl = imageDto.ImageUrl,
                            AltText = imageDto.AltText,
                            IsPrimary = shouldBePrimary,
                            DisplayOrder = imageDto.DisplayOrder,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Repository<ProductImage>().AddAsync(image);
                    }
                }

                var incomingPriceLevelIds = new HashSet<Guid>(incomingPrices.Select(p => p.PriceLevelId));
                foreach (var price in existingPrices.Where(p => !incomingPriceLevelIds.Contains(p.PriceLevelId)))
                {
                    await _unitOfWork.ProductPrices.DeleteAsync(price.Id);
                }

                var incomingImageUrls = new HashSet<string>(incomingImages.Select(i => i.ImageUrl));
                foreach (var image in existingImages.Where(i => !incomingImageUrls.Contains(i.ImageUrl)))
                {
                    await _unitOfWork.Repository<ProductImage>().DeleteAsync(image.Id);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        });

        return NoContent();
    }
}

