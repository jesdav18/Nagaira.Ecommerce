using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("Auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly int _refreshTokenDays;
    private readonly string? _refreshCookieDomain;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _refreshTokenDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out var days) ? days : 30;
        _refreshCookieDomain = configuration["Jwt:RefreshCookieDomain"];
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterUserDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _authService.RegisterAsync(dto);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(result.Response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _authService.LoginAsync(dto);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(result.Response);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            return Unauthorized(new { message = "Refresh token missing" });
        }

        try
        {
            var result = await _authService.RefreshAsync(refreshToken);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(result.Response);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            await _authService.RevokeRefreshTokenAsync(refreshToken);
        }

        var deleteOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            Path = "/"
        };
        if (!string.IsNullOrWhiteSpace(_refreshCookieDomain))
        {
            deleteOptions.Domain = _refreshCookieDomain;
        }

        Response.Cookies.Append("refreshToken", string.Empty, deleteOptions);

        return Ok(new { message = "Sesión cerrada" });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            // Helper simple para obtener ID del usuario autenticado
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null) return Unauthorized();

            await _authService.ChangePasswordAsync(Guid.Parse(userIdClaim.Value), dto);
            return Ok(new { message = "Contraseña actualizada exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(_refreshTokenDays),
            Path = "/"
        };
        if (!string.IsNullOrWhiteSpace(_refreshCookieDomain))
        {
            cookieOptions.Domain = _refreshCookieDomain;
        }

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
