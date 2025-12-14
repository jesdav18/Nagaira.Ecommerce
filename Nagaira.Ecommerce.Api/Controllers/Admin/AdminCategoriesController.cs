using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using System.Text.Json;
using Nagaira.Ecommerce.Api.Helpers;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IAuditService _auditService;

    public AdminCategoriesController(
        ICategoryService categoryService,
        IAuditService auditService)
    {
        _categoryService = categoryService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        var categories = await _categoryService.GetAllCategoriesForAdminAsync();
        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id)
    {
        var category = await _categoryService.GetCategoryByIdForAdminAsync(id);
        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var category = await _categoryService.CreateCategoryAsync(dto);
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "CREATE",
                "Category",
                category.Id,
                null,
                JsonSerializer.Serialize(dto)
            );
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var existingCategory = await _categoryService.GetCategoryByIdForAdminAsync(id);
            var oldValues = existingCategory != null ? JsonSerializer.Serialize(existingCategory) : null;
            
            await _categoryService.UpdateCategoryAsync(id, dto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "UPDATE",
                "Category",
                id,
                oldValues,
                JsonSerializer.Serialize(dto)
            );
            
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var existingCategory = await _categoryService.GetCategoryByIdForAdminAsync(id);
            var oldValues = existingCategory != null ? JsonSerializer.Serialize(existingCategory) : null;
            
            await _categoryService.DeleteCategoryAsync(id);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DELETE",
                "Category",
                id,
                oldValues,
                null
            );
            
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdForAdminAsync(id);
            if (category == null) return NotFound();

            var updateDto = new UpdateCategoryDto(
                category.Name,
                category.Description,
                category.ImageUrl,
                category.ParentCategoryId,
                true
            );
            await _categoryService.UpdateCategoryAsync(id, updateDto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "ACTIVATE",
                "Category",
                id
            );
            
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdForAdminAsync(id);
            if (category == null) return NotFound();

            var updateDto = new UpdateCategoryDto(
                category.Name,
                category.Description,
                category.ImageUrl,
                category.ParentCategoryId,
                false
            );
            await _categoryService.UpdateCategoryAsync(id, updateDto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DEACTIVATE",
                "Category",
                id
            );
            
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

