using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
[Route("api/app-settings")]
public class AppSettingsController : ControllerBase
{
    private readonly IAppSettingService _appSettingService;

    public AppSettingsController(IAppSettingService appSettingService)
    {
        _appSettingService = appSettingService;
    }

    [HttpGet("active")]
    public async Task<ActionResult> GetActiveSettings()
    {
        var settings = await _appSettingService.GetAllSettingsAsync();
        var activeSettings = settings.Where(s => s.IsActive);
        return Ok(activeSettings);
    }

    [HttpGet("value/{key}")]
    public async Task<ActionResult> GetSettingValue(string key)
    {
        var value = await _appSettingService.GetSettingValueAsync(key);
        if (value == null) return NotFound();
        return Ok(new { key, value });
    }
}

