using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Application.MetaCatalog;

namespace Nagaira.Ecommerce.Infrastructure.Integrations.MetaCatalog;

public class MetaCatalogClient : IMetaCatalogClient
{
    public const string HttpClientName = "MetaCatalog";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MetaCatalogOptions _options;

    public MetaCatalogClient(IHttpClientFactory httpClientFactory, IOptions<MetaCatalogOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<MetaCatalogBatchResult> SubmitAsync(
        IReadOnlyCollection<MetaCatalogMappingResult> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return new MetaCatalogBatchResult(Array.Empty<MetaCatalogItemResult>());
        }

        if (!_options.SyncEnabled)
        {
            return new MetaCatalogBatchResult(items.Select(i => new MetaCatalogItemResult(
                i.RetailerId,
                i.Action,
                true,
                null,
                null,
                "Meta catalog sync is disabled.",
                false)).ToList());
        }

        ValidateReadyForCall();

        var results = new List<MetaCatalogItemResult>();
        var batchSize = Math.Max(1, _options.BatchSize);
        foreach (var batch in items.Chunk(batchSize))
        {
            var response = await SubmitBatchAsync(batch, cancellationToken);
            results.AddRange(response.Items);
        }

        return new MetaCatalogBatchResult(results);
    }

    private async Task<MetaCatalogBatchResult> SubmitBatchAsync(
        IReadOnlyCollection<MetaCatalogMappingResult> items,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds)));

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildItemsBatchUrl());
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(BuildRequest(items), JsonOptions),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            throw new MetaCatalogApiException(
                null,
                "Meta Catalog request timed out.",
                true,
                innerException: ex);
        }
        catch (TimeoutException ex)
        {
            throw new MetaCatalogApiException(
                null,
                "Meta Catalog request timed out.",
                true,
                innerException: ex);
        }

        using var _ = response;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw BuildHttpException(response.StatusCode, body);
        }

        return ParseSuccessResponse(items, body);
    }

    private Uri BuildItemsBatchUrl()
    {
        var baseUrl = _options.ApiBaseUrl.TrimEnd('/');
        var version = _options.GraphApiVersion.Trim().Trim('/');
        var catalogId = Uri.EscapeDataString(_options.CatalogId.Trim());
        return new Uri($"{baseUrl}/{version}/{catalogId}/items_batch");
    }

    private static MetaCatalogBatchRequest BuildRequest(IEnumerable<MetaCatalogMappingResult> items)
    {
        return new MetaCatalogBatchRequest
        {
            Requests = items.Select(item => new MetaCatalogBatchRequestItem
            {
                Method = item.Action == MetaCatalogSyncAction.Upsert ? "UPDATE" : "DELETE",
                RetailerId = item.RetailerId,
                Data = item.Action == MetaCatalogSyncAction.Upsert && item.Item != null
                    ? new MetaCatalogItemData
                    {
                        Name = item.Item.Name,
                        Description = item.Item.Description,
                        Brand = item.Item.Brand,
                        Availability = item.Item.Availability,
                        Condition = item.Item.Condition,
                        Price = item.Item.Price,
                        Currency = item.Item.Currency,
                        Link = item.Item.Url,
                        ImageLink = item.Item.ImageUrl,
                        ProductType = item.Item.CategoryName,
                        Sku = item.Item.Sku
                    }
                    : null
            }).ToList()
        };
    }

    private static MetaCatalogBatchResult ParseSuccessResponse(
        IReadOnlyCollection<MetaCatalogMappingResult> requestedItems,
        string body)
    {
        MetaCatalogBatchResponse? parsed;
        try
        {
            parsed = string.IsNullOrWhiteSpace(body)
                ? new MetaCatalogBatchResponse()
                : JsonSerializer.Deserialize<MetaCatalogBatchResponse>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new MetaCatalogApiException(
                HttpStatusCode.OK,
                "Meta Catalog returned an invalid JSON response.",
                false,
                innerException: ex);
        }

        var responseItems = parsed?.Responses ?? parsed?.Items ?? parsed?.Results;
        if (responseItems == null || responseItems.Count == 0)
        {
            return new MetaCatalogBatchResult(requestedItems.Select(i => new MetaCatalogItemResult(
                i.RetailerId,
                i.Action,
                true,
                null,
                null,
                null,
                false)).ToList());
        }

        var actionByRetailerId = requestedItems.ToDictionary(i => i.RetailerId, i => i.Action);
        var results = responseItems.Select(item =>
        {
            var retailerId = item.RetailerId ?? string.Empty;
            var action = actionByRetailerId.GetValueOrDefault(retailerId, MetaCatalogSyncAction.Upsert);
            var success = item.Success ?? string.Equals(item.Status, "success", StringComparison.OrdinalIgnoreCase);
            var errorCode = item.Error?.Code;
            return new MetaCatalogItemResult(
                retailerId,
                action,
                success,
                item.Id,
                errorCode,
                item.Error?.Message,
                item.Error?.IsTransient ?? IsTransientErrorCode(errorCode));
        }).ToList();

        return new MetaCatalogBatchResult(results);
    }

    private static MetaCatalogApiException BuildHttpException(HttpStatusCode statusCode, string body)
    {
        MetaCatalogError? error = null;
        try
        {
            error = JsonSerializer.Deserialize<MetaCatalogErrorResponse>(body, JsonOptions)?.Error;
        }
        catch (JsonException)
        {
            // Keep a sanitized generic error below.
        }

        var isTransient = statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
        var safeMessage = statusCode switch
        {
            HttpStatusCode.BadRequest => "Meta Catalog rejected the request payload.",
            HttpStatusCode.Unauthorized => "Meta Catalog authentication failed.",
            HttpStatusCode.Forbidden => "Meta Catalog authorization failed.",
            HttpStatusCode.TooManyRequests => "Meta Catalog rate limit exceeded.",
            _ when (int)statusCode >= 500 => "Meta Catalog returned a transient server error.",
            _ => "Meta Catalog request failed."
        };

        return new MetaCatalogApiException(
            statusCode,
            safeMessage,
            isTransient,
            error?.Code?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            error?.ErrorSubcode?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            error?.FbTraceId);
    }

    private void ValidateReadyForCall()
    {
        var validator = new MetaCatalogOptionsValidator();
        var validation = validator.Validate(null, _options);
        if (validation.Failed)
        {
            throw new MetaCatalogApiException(
                null,
                "Meta Catalog configuration is incomplete.",
                false);
        }
    }

    private static bool IsTransientErrorCode(string? code)
    {
        return int.TryParse(code, out var parsed) && (parsed == 429 || parsed >= 500);
    }
}
