using Microsoft.Extensions.Options;

namespace Nagaira.Ecommerce.Api.WhatsApp;

public class WhatsAppClientSelector : IWhatsAppClient
{
    private readonly MetaWhatsAppClient _meta;
    private readonly TwilioWhatsAppClient _twilio;
    private readonly WhatsAppOptions _options;

    public WhatsAppClientSelector(
        MetaWhatsAppClient meta,
        TwilioWhatsAppClient twilio,
        IOptions<WhatsAppOptions> options)
    {
        _meta = meta;
        _twilio = twilio;
        _options = options.Value;
    }

    private IWhatsAppClient Current =>
        _options.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase) ? _twilio : _meta;

    public Task SendTextAsync(string to, string text, CancellationToken ct = default)
        => Current.SendTextAsync(to, text, ct);

    public Task SendButtonsAsync(string to, string text, IEnumerable<(string Id, string Title)> buttons, CancellationToken ct = default)
        => Current.SendButtonsAsync(to, text, buttons, ct);

    public Task SendListAsync(string to, string text, string buttonText, IEnumerable<WhatsAppListSection> sections, CancellationToken ct = default)
        => Current.SendListAsync(to, text, buttonText, sections, ct);

    public Task SendImageAsync(string to, string imageUrl, string? caption = null, CancellationToken ct = default)
        => Current.SendImageAsync(to, imageUrl, caption, ct);
}
