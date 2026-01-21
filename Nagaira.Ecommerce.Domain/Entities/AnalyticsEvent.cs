namespace Nagaira.Ecommerce.Domain.Entities;

public class AnalyticsEvent : BaseEntity
{
    public string EventName { get; set; } = string.Empty;
    public string AnonUserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Referrer { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmTerm { get; set; }
    public string? UtmContent { get; set; }
    public string? OrderId { get; set; }
    public decimal? Value { get; set; }
    public string? Currency { get; set; }
    public string? Meta { get; set; }
    public string? VisitorHash { get; set; }
}
