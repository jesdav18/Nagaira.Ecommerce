using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using System.Text.Json;
using Nagaira.Ecommerce.Api.Helpers;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/app-settings")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminAppSettingsController : ControllerBase
{
    private readonly IAppSettingService _appSettingService;
    private readonly IAuditService _auditService;

    public AdminAppSettingsController(IAppSettingService appSettingService, IAuditService auditService)
    {
        _appSettingService = appSettingService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppSettingDto>>> GetAll()
    {
        var settings = await _appSettingService.GetAllSettingsAsync();
        return Ok(settings);
    }

    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<AppSettingDto>>> GetByCategory(string category)
    {
        var settings = await _appSettingService.GetSettingsByCategoryAsync(category);
        return Ok(settings);
    }

    [HttpGet("key/{key}")]
    public async Task<ActionResult<AppSettingDto>> GetByKey(string key)
    {
        var setting = await _appSettingService.GetSettingByKeyAsync(key);
        if (setting == null) return NotFound();
        return Ok(setting);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppSettingDto>> GetById(Guid id)
    {
        var settings = await _appSettingService.GetAllSettingsAsync();
        var setting = settings.FirstOrDefault(s => s.Id == id);
        if (setting == null) return NotFound();
        return Ok(setting);
    }

    [HttpPost]
    public async Task<ActionResult<AppSettingDto>> Create([FromBody] CreateAppSettingDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var setting = await _appSettingService.CreateSettingAsync(dto);
            
            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "CREATE",
                "AppSetting",
                setting.Id,
                null,
                JsonSerializer.Serialize(dto)
            );

            return CreatedAtAction(nameof(GetById), new { id = setting.Id }, setting);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAppSettingDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var settings = await _appSettingService.GetAllSettingsAsync();
            var existing = settings.FirstOrDefault(s => s.Id == id);
            var oldValues = existing != null ? JsonSerializer.Serialize(existing) : null;

            await _appSettingService.UpdateSettingAsync(dto);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "UPDATE",
                "AppSetting",
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
            var settings = await _appSettingService.GetAllSettingsAsync();
            var existing = settings.FirstOrDefault(s => s.Id == id);
            var oldValues = existing != null ? JsonSerializer.Serialize(existing) : null;

            await _appSettingService.DeleteSettingAsync(id);

            await AuditHelper.LogAdminActionAsync(
                _auditService,
                HttpContext,
                "DELETE",
                "AppSetting",
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
}

