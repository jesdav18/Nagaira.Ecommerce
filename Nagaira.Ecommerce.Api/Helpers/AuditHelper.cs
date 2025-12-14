using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.Helpers;

public static class AuditHelper
{
    public static async Task LogAdminActionAsync(
        IAuditService auditService,
        HttpContext httpContext,
        string action,
        string entityType,
        Guid? entityId = null,
        string? oldValues = null,
        string? newValues = null)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            
            await auditService.LogActionAsync(
                userId,
                action,
                entityType,
                entityId,
                oldValues,
                newValues,
                ipAddress,
                userAgent
            );
        }
    }
}

