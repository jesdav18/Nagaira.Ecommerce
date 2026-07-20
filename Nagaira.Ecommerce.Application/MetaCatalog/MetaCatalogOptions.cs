namespace Nagaira.Ecommerce.Application.MetaCatalog;

public class MetaCatalogOptions
{
    public string ApiBaseUrl { get; set; } = "https://graph.facebook.com";
    public string GraphApiVersion { get; set; } = string.Empty;
    public string CatalogId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string Currency { get; set; } = "HNL";
    public string PublicBaseUrl { get; set; } = string.Empty;
    public Guid? PublicPriceLevelId { get; set; }
    public bool SyncEnabled { get; set; }
    public int BatchSize { get; set; } = 100;
    public int LockMinutes { get; set; } = 10;
    public int RequestTimeoutSeconds { get; set; } = 30;
}
