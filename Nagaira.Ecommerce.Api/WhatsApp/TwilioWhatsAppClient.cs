using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Nagaira.Ecommerce.Api.WhatsApp;

public class TwilioWhatsAppClient : IWhatsAppClient
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<TwilioWhatsAppClient> _logger;

    public TwilioWhatsAppClient(HttpClient httpClient, IOptions<WhatsAppOptions> options, ILogger<TwilioWhatsAppClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task SendTextAsync(string to, string text, CancellationToken ct = default)
    {
        return SendMessageAsync(to, text, null, ct);
    }

    public Task SendButtonsAsync(string to, string text, IEnumerable<(string Id, string Title)> buttons, CancellationToken ct = default)
    {
        var options = buttons.Select((b, i) => $"{i + 1}) {b.Title}").ToList();
        var body = text + "\n" + string.Join("\n", options) + "\nResponde con el numero o escribi MENU.";
        return SendMessageAsync(to, body, null, ct);
    }

    public Task SendListAsync(string to, string text, string buttonText, IEnumerable<WhatsAppListSection> sections, CancellationToken ct = default)
    {
        var lines = new List<string> { text };
        foreach (var section in sections)
        {
            if (!string.IsNullOrWhiteSpace(section.Title))
            {
                lines.Add($"-- {section.Title} --");
            }
            foreach (var row in section.Rows)
            {
                var desc = string.IsNullOrWhiteSpace(row.Description) ? string.Empty : $" ({row.Description})";
                lines.Add($"{row.Id}: {row.Title}{desc}");
            }
        }
        lines.Add("Responde con el codigo.");
        return SendMessageAsync(to, string.Join("\n", lines), null, ct);
    }

    public Task SendImageAsync(string to, string imageUrl, string? caption = null, CancellationToken ct = default)
    {
        return SendMessageAsync(to, caption ?? string.Empty, new[] { imageUrl }, ct);
    }

    private Task SendMessageAsync(string to, string body, IEnumerable<string>? mediaUrls, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.TwilioAccountSid) ||
            string.IsNullOrWhiteSpace(_options.TwilioAuthToken) ||
            string.IsNullOrWhiteSpace(_options.TwilioFrom))
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Twilio send From={From} To={To}", _options.TwilioFrom, to);

        var url = $"https://api.twilio.com/2010-04-01/Accounts/{_options.TwilioAccountSid}/Messages.json";
        var req = new HttpRequestMessage(HttpMethod.Post, url);
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.TwilioAccountSid}:{_options.TwilioAuthToken}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

        var form = new List<KeyValuePair<string, string>>
        {
            new("From", _options.TwilioFrom),
            new("To", to.StartsWith("whatsapp:") ? to : $"whatsapp:{to}")
        };

        if (!string.IsNullOrWhiteSpace(body))
        {
            form.Add(new KeyValuePair<string, string>("Body", body));
        }

        if (mediaUrls != null)
        {
            foreach (var media in mediaUrls)
            {
                if (!string.IsNullOrWhiteSpace(media))
                {
                    form.Add(new KeyValuePair<string, string>("MediaUrl", media));
                }
            }
        }

        req.Content = new FormUrlEncodedContent(form);
        return SendAndLogAsync(req, ct);
    }

    private async Task SendAndLogAsync(HttpRequestMessage req, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.SendAsync(req, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("Twilio response {StatusCode}: {Body}", (int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Twilio");
        }
        finally
        {
            req.Dispose();
        }
    }
}
