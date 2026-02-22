using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nagaira.Ecommerce.Api.WhatsApp;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
[Route("api/whatsapp")]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppBot _bot;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(IWhatsAppBot bot, IOptions<WhatsAppOptions> options, ILogger<WhatsAppController> logger)
    {
        _bot = bot;
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet("webhook")]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (mode == "subscribe" && token == _options.VerifyToken)
        {
            return Ok(challenge);
        }
        return Unauthorized();
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        try
        {
            IncomingMessage? incoming = null;

            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                var from = form["From"].ToString();
                var body = form["Body"].ToString();
                if (!string.IsNullOrWhiteSpace(from))
                {
                    if (from.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
                    {
                        from = from["whatsapp:".Length..];
                    }
                    incoming = new IncomingMessage(from, body, null, null);
                    _logger.LogInformation("Twilio inbound from {From} body {Body}", from, body);
                }
            }
            else if (Request.ContentLength.HasValue && Request.ContentLength.Value > 0)
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    using var doc = JsonDocument.Parse(body);
                    incoming = ParseIncomingMessage(doc.RootElement);
                    _logger.LogInformation("Meta inbound parsed {HasIncoming}", incoming != null);
                }
            }

            if (incoming != null)
            {
                _logger.LogInformation("Dispatching bot for {From}", incoming.From);
                await _bot.HandleMessageAsync(incoming);
            }
        }
        catch
        {
            // Always return 200 to avoid webhook retries on malformed payloads.
        }

        return Ok();
    }

    private static IncomingMessage? ParseIncomingMessage(JsonElement payload)
    {
        if (!payload.TryGetProperty("entry", out var entry) || entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() == 0)
        {
            return null;
        }

        if (!entry[0].TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array || changes.GetArrayLength() == 0)
        {
            return null;
        }

        if (!changes[0].TryGetProperty("value", out var value))
        {
            return null;
        }

        if (!value.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array || messages.GetArrayLength() == 0)
        {
            return null;
        }

        var msg = messages[0];
        var from = msg.TryGetProperty("from", out var fromProp) ? fromProp.GetString() : null;
        string? text = null;
        string? buttonId = null;
        string? listId = null;

        if (msg.TryGetProperty("text", out var textObj))
        {
            text = textObj.GetProperty("body").GetString();
        }

        if (msg.TryGetProperty("interactive", out var interactive))
        {
            if (interactive.TryGetProperty("button_reply", out var buttonReply))
            {
                buttonId = buttonReply.GetProperty("id").GetString();
            }
            if (interactive.TryGetProperty("list_reply", out var listReply))
            {
                listId = listReply.GetProperty("id").GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(from))
        {
            return null;
        }

        return new IncomingMessage(from!, text, buttonId, listId);
    }
}
