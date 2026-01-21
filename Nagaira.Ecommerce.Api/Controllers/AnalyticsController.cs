using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private static readonly HashSet<string> AllowedEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "page_view",
        "view_product",
        "add_to_cart",
        "begin_checkout",
        "purchase"
    };

    public AnalyticsController(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    [HttpPost("event")]
    public async Task<IActionResult> TrackEvent([FromBody] AnalyticsEventRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Payload invalido." });
        }

        if (string.IsNullOrWhiteSpace(request.EventName) ||
            string.IsNullOrWhiteSpace(request.AnonUserId) ||
            string.IsNullOrWhiteSpace(request.SessionId))
        {
            return BadRequest(new { message = "eventName, anonUserId y sessionId son obligatorios." });
        }

        if (!AllowedEvents.Contains(request.EventName))
        {
            return BadRequest(new { message = "Evento no valido." });
        }

        var eventName = request.EventName.Trim().ToLowerInvariant();
        var orderId = TrimTo(request.OrderId, 100);

        if (eventName == "purchase" && !string.IsNullOrWhiteSpace(orderId))
        {
            var existing = await _unitOfWork.Repository<AnalyticsEvent>().FindAsync(e =>
                e.OrderId == orderId && e.EventName == "purchase" && !e.IsDeleted);
            if (existing.Any())
            {
                return NoContent();
            }
        }

        var entity = new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            EventName = TrimTo(eventName, 50) ?? eventName,
            AnonUserId = TrimTo(request.AnonUserId, 100) ?? string.Empty,
            SessionId = TrimTo(request.SessionId, 100) ?? string.Empty,
            Path = TrimTo(request.Path, 500),
            Referrer = TrimTo(request.Referrer, 500),
            UtmSource = TrimTo(request.UtmSource, 100),
            UtmMedium = TrimTo(request.UtmMedium, 100),
            UtmCampaign = TrimTo(request.UtmCampaign, 100),
            UtmTerm = TrimTo(request.UtmTerm, 100),
            UtmContent = TrimTo(request.UtmContent, 100),
            OrderId = orderId,
            Value = request.Value,
            Currency = TrimTo(request.Currency, 10),
            Meta = request.Meta.HasValue ? request.Meta.Value.GetRawText() : null,
            VisitorHash = GetVisitorHash(),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Repository<AnalyticsEvent>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return Accepted();
    }

    private string? GetVisitorHash()
    {
        var secret = _configuration["Analytics:HashSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var userAgent = Request.Headers.UserAgent.ToString() ?? string.Empty;
        var raw = $"{ip}|{userAgent}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? TrimTo(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

public record AnalyticsEventRequest(
    string EventName,
    string AnonUserId,
    string SessionId,
    string? Path,
    string? Referrer,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    string? UtmTerm,
    string? UtmContent,
    string? OrderId,
    decimal? Value,
    string? Currency,
    JsonElement? Meta
);
