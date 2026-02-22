using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Nagaira.Ecommerce.Api.WhatsApp;

public interface IWhatsAppClient
{
    Task SendTextAsync(string to, string text, CancellationToken ct = default);
    Task SendButtonsAsync(string to, string text, IEnumerable<(string Id, string Title)> buttons, CancellationToken ct = default);
    Task SendListAsync(string to, string text, string buttonText, IEnumerable<WhatsAppListSection> sections, CancellationToken ct = default);
    Task SendImageAsync(string to, string imageUrl, string? caption = null, CancellationToken ct = default);
}

public class WhatsAppListSection
{
    public string Title { get; set; } = string.Empty;
    public List<WhatsAppListRow> Rows { get; set; } = new();
}

public class WhatsAppListRow
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class MetaWhatsAppClient : IWhatsAppClient
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;

    public MetaWhatsAppClient(HttpClient httpClient, IOptions<WhatsAppOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public Task SendTextAsync(string to, string text, CancellationToken ct = default)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { body = text }
        };
        return SendAsync(payload, ct);
    }

    public Task SendButtonsAsync(string to, string text, IEnumerable<(string Id, string Title)> buttons, CancellationToken ct = default)
    {
        var btns = buttons.Take(3)
            .Select(b => new
            {
                type = "reply",
                reply = new { id = b.Id, title = b.Title }
            })
            .ToList();

        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "interactive",
            interactive = new
            {
                type = "button",
                body = new { text },
                action = new { buttons = btns }
            }
        };
        return SendAsync(payload, ct);
    }

    public Task SendListAsync(string to, string text, string buttonText, IEnumerable<WhatsAppListSection> sections, CancellationToken ct = default)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "interactive",
            interactive = new
            {
                type = "list",
                body = new { text },
                action = new
                {
                    button = buttonText,
                    sections = sections.Select(s => new
                    {
                        title = s.Title,
                        rows = s.Rows.Select(r => new
                        {
                            id = r.Id,
                            title = r.Title,
                            description = r.Description
                        })
                    })
                }
            }
        };
        return SendAsync(payload, ct);
    }

    public Task SendImageAsync(string to, string imageUrl, string? caption = null, CancellationToken ct = default)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "image",
            image = new { link = imageUrl, caption }
        };
        return SendAsync(payload, ct);
    }

    private Task SendAsync(object payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken) || string.IsNullOrWhiteSpace(_options.PhoneNumberId))
        {
            return Task.CompletedTask;
        }

        var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/{_options.PhoneNumberId}/messages";
        var json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return _httpClient.SendAsync(req, ct);
    }
}
