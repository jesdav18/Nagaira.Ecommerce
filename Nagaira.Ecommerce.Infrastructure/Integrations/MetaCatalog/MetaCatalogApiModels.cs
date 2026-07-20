using System.Text.Json.Serialization;

namespace Nagaira.Ecommerce.Infrastructure.Integrations.MetaCatalog;

internal sealed class MetaCatalogBatchRequest
{
    [JsonPropertyName("requests")]
    public List<MetaCatalogBatchRequestItem> Requests { get; set; } = new();
}

internal sealed class MetaCatalogBatchRequestItem
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("retailer_id")]
    public string RetailerId { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MetaCatalogItemData? Data { get; set; }
}

internal sealed class MetaCatalogItemData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("brand")]
    public string Brand { get; set; } = string.Empty;

    [JsonPropertyName("availability")]
    public string Availability { get; set; } = string.Empty;

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;

    [JsonPropertyName("image_link")]
    public string ImageLink { get; set; } = string.Empty;

    [JsonPropertyName("product_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProductType { get; set; }

    [JsonPropertyName("retailer_product_group_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sku { get; set; }
}

internal sealed class MetaCatalogBatchResponse
{
    [JsonPropertyName("responses")]
    public List<MetaCatalogBatchResponseItem>? Responses { get; set; }

    [JsonPropertyName("items")]
    public List<MetaCatalogBatchResponseItem>? Items { get; set; }

    [JsonPropertyName("results")]
    public List<MetaCatalogBatchResponseItem>? Results { get; set; }
}

internal sealed class MetaCatalogBatchResponseItem
{
    [JsonPropertyName("retailer_id")]
    public string? RetailerId { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("success")]
    public bool? Success { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error")]
    public MetaCatalogBatchItemError? Error { get; set; }
}

internal sealed class MetaCatalogBatchItemError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("is_transient")]
    public bool? IsTransient { get; set; }
}
