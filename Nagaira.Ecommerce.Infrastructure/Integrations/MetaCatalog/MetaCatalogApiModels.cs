using System.Text.Json.Serialization;

namespace Nagaira.Ecommerce.Infrastructure.Integrations.MetaCatalog;

internal sealed class MetaCatalogBatchRequest
{
    [JsonPropertyName("item_type")]
    public string ItemType { get; set; } = "PRODUCT_ITEM";

    [JsonPropertyName("requests")]
    public List<MetaCatalogBatchRequestItem> Requests { get; set; } = new();
}

internal sealed class MetaCatalogBatchRequestItem
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonIgnore]
    public string RetailerId { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MetaCatalogItemData? Data { get; set; }
}

internal sealed class MetaCatalogItemData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("brand")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Brand { get; set; }

    [JsonPropertyName("availability")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Availability { get; set; }

    [JsonPropertyName("condition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Condition { get; set; }

    [JsonPropertyName("price")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Price { get; set; }

    [JsonPropertyName("currency")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Currency { get; set; }

    [JsonPropertyName("link")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }

    [JsonPropertyName("image_link")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageLink { get; set; }

    [JsonPropertyName("product_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProductType { get; set; }

    [JsonPropertyName("retailer_product_group_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sku { get; set; }
}

internal sealed class MetaCatalogBatchResponse
{
    [JsonPropertyName("handles")]
    public List<string>? Handles { get; set; }

    [JsonPropertyName("handle")]
    public string? Handle { get; set; }

    [JsonPropertyName("validation_status")]
    public List<MetaCatalogBatchValidationStatus>? ValidationStatus { get; set; }

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

internal sealed class MetaCatalogBatchValidationStatus
{
    [JsonPropertyName("retailer_id")]
    public string? RetailerId { get; set; }

    [JsonPropertyName("errors")]
    public List<MetaCatalogBatchItemError>? Errors { get; set; }

    [JsonPropertyName("warnings")]
    public List<MetaCatalogBatchItemWarning>? Warnings { get; set; }
}

internal sealed class MetaCatalogBatchItemError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("is_transient")]
    public bool? IsTransient { get; set; }

    [JsonPropertyName("error_subcode")]
    public string? ErrorSubcode { get; set; }
}

internal sealed class MetaCatalogBatchItemWarning
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

internal sealed class MetaCatalogBatchStatusResponse
{
    [JsonPropertyName("data")]
    public List<MetaCatalogBatchStatusItem>? Data { get; set; }

    [JsonPropertyName("handle")]
    public string? Handle { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("errors")]
    public List<MetaCatalogBatchValidationStatus>? Errors { get; set; }

    [JsonPropertyName("errors_total_count")]
    public int? ErrorsTotalCount { get; set; }

    [JsonPropertyName("warnings")]
    public List<MetaCatalogBatchValidationStatus>? Warnings { get; set; }

    [JsonPropertyName("warnings_total_count")]
    public int? WarningsTotalCount { get; set; }

    [JsonPropertyName("ids_of_invalid_requests")]
    public List<string>? IdsOfInvalidRequests { get; set; }
}

internal sealed class MetaCatalogBatchStatusItem
{
    [JsonPropertyName("handle")]
    public string? Handle { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("errors")]
    public List<MetaCatalogBatchValidationStatus>? Errors { get; set; }

    [JsonPropertyName("errors_total_count")]
    public int? ErrorsTotalCount { get; set; }

    [JsonPropertyName("warnings")]
    public List<MetaCatalogBatchValidationStatus>? Warnings { get; set; }

    [JsonPropertyName("warnings_total_count")]
    public int? WarningsTotalCount { get; set; }

    [JsonPropertyName("ids_of_invalid_requests")]
    public List<string>? IdsOfInvalidRequests { get; set; }
}
