namespace Nagaira.Ecommerce.Api.WhatsApp;

public class WhatsAppOptions
{
    public string Provider { get; set; } = "Meta";
    public string VerifyToken { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string BusinessAccountId { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://graph.facebook.com/v20.0";
    public bool BotEnabled { get; set; } = true;
    public bool HumanHandoffEnabled { get; set; } = true;
    public string HumanHandoffKeywords { get; set; } = "asesor,vendedor,humano";
    public int HumanHandoffPauseMinutes { get; set; } = 120;

    public string TwilioAccountSid { get; set; } = string.Empty;
    public string TwilioAuthToken { get; set; } = string.Empty;
    public string TwilioFrom { get; set; } = string.Empty;
}
