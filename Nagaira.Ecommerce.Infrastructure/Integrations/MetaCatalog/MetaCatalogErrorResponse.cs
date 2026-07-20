using System.Text.Json.Serialization;

namespace Nagaira.Ecommerce.Infrastructure.Integrations.MetaCatalog;

public class MetaCatalogErrorResponse
{
    [JsonPropertyName("error")]
    public MetaCatalogError? Error { get; set; }
}

public class MetaCatalogError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("error_subcode")]
    public int? ErrorSubcode { get; set; }

    [JsonPropertyName("fbtrace_id")]
    public string? FbTraceId { get; set; }
}
