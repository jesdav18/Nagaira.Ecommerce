using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<MetaCatalogClient> _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public MetaCatalogClient(
        IHttpClientFactory httpClientFactory,
        IOptions<MetaCatalogOptions> options,
        ILogger<MetaCatalogClient> logger,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _delay = delay ?? Task.Delay;
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
            throw BuildHttpException(
                response.StatusCode,
                body,
                response.Content.Headers.ContentType?.ToString(),
                _options.AccessToken);
        }

        return await ParseAcceptedBatchResponseAsync(items, body, cancellationToken);
    }

    private Uri BuildItemsBatchUrl()
    {
        var baseUrl = _options.ApiBaseUrl.TrimEnd('/');
        var version = _options.GraphApiVersion.Trim().Trim('/');
        var catalogId = Uri.EscapeDataString(_options.CatalogId.Trim());
        return new Uri($"{baseUrl}/{version}/{catalogId}/items_batch");
    }

    private Uri BuildCheckBatchStatusUrl(string handle)
    {
        var baseUrl = _options.ApiBaseUrl.TrimEnd('/');
        var version = _options.GraphApiVersion.Trim().Trim('/');
        var catalogId = Uri.EscapeDataString(_options.CatalogId.Trim());
        var escapedHandle = Uri.EscapeDataString(handle);
        return new Uri($"{baseUrl}/{version}/{catalogId}/check_batch_request_status?handle={escapedHandle}");
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

    private async Task<MetaCatalogBatchResult> ParseAcceptedBatchResponseAsync(
        IReadOnlyCollection<MetaCatalogMappingResult> requestedItems,
        string body,
        CancellationToken cancellationToken)
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

        var validationResult = BuildValidationResult(requestedItems, parsed?.ValidationStatus, null, null);
        if (validationResult != null)
        {
            return validationResult;
        }

        var directResult = BuildDirectResponseResult(requestedItems, parsed);
        if (directResult != null)
        {
            return directResult;
        }

        var handles = (parsed?.Handles ?? (string.IsNullOrWhiteSpace(parsed?.Handle) ? null : [parsed.Handle]))
            ?.Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (handles == null || handles.Count == 0)
        {
            return new MetaCatalogBatchResult(requestedItems.Select(i => new MetaCatalogItemResult(
                i.RetailerId,
                i.Action,
                false,
                null,
                null,
                "Meta Catalog batch response did not include a handle.",
                true,
                "missing_handle")).ToList());
        }

        if (handles.Count == 1)
        {
            return await PollBatchStatusAsync(requestedItems, handles[0], cancellationToken);
        }

        return await PollBatchStatusesAsync(requestedItems, handles, cancellationToken);
    }

    private MetaCatalogBatchResult? BuildValidationResult(
        IReadOnlyCollection<MetaCatalogMappingResult> requestedItems,
        List<MetaCatalogBatchValidationStatus>? validationStatuses,
        string? batchHandle,
        string? status)
    {
        if (validationStatuses == null || validationStatuses.Count == 0)
        {
            return null;
        }

        var validationByRetailerId = validationStatuses
            .Where(v => !string.IsNullOrWhiteSpace(v.RetailerId))
            .GroupBy(v => v.RetailerId!)
            .ToDictionary(g => g.Key, g => g.First());

        var hasErrors = validationByRetailerId.Values.Any(v => v.Errors?.Count > 0);
        if (!hasErrors)
        {
            return null;
        }

        return new MetaCatalogBatchResult(requestedItems.Select(item =>
        {
            validationByRetailerId.TryGetValue(item.RetailerId, out var validation);
            var firstError = validation?.Errors?.FirstOrDefault();
            var warnings = validation?.Warnings?
                .Select(w => w.Message)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Cast<string>()
                .ToList();

            return new MetaCatalogItemResult(
                item.RetailerId,
                item.Action,
                firstError == null,
                null,
                firstError?.Code,
                SanitizeMetaMessage(firstError?.Message, _options.AccessToken),
                firstError?.IsTransient ?? false,
                status,
                firstError?.ErrorSubcode,
                warnings,
                batchHandle);
        }).ToList());
    }

    private MetaCatalogBatchResult? BuildDirectResponseResult(
        IReadOnlyCollection<MetaCatalogMappingResult> requestedItems,
        MetaCatalogBatchResponse? parsed)
    {
        var responseItems = parsed?.Responses ?? parsed?.Items ?? parsed?.Results;
        if (responseItems == null)
        {
            return null;
        }

        if (responseItems.Count == 0)
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
                SanitizeMetaMessage(item.Error?.Message, _options.AccessToken),
                item.Error?.IsTransient ?? IsTransientErrorCode(errorCode),
                item.Status);
        }).ToList();

        return new MetaCatalogBatchResult(results);
    }

    private async Task<MetaCatalogBatchResult> PollBatchStatusAsync(
        IReadOnlyCollection<MetaCatalogMappingResult> requestedItems,
        string handle,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var statusResult = await GetBatchStatusAsync(requestedItems, handle, cancellationToken);
            if (statusResult.IsTerminal)
            {
                return statusResult.Result;
            }

            if (attempt < 5)
            {
                await _delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        return new MetaCatalogBatchResult(requestedItems.Select(i => new MetaCatalogItemResult(
            i.RetailerId,
            i.Action,
            false,
            null,
            null,
            "Meta Catalog batch is still processing.",
            true,
            "processing",
            null,
            null,
            handle)).ToList());
    }

    private async Task<MetaCatalogBatchResult> PollBatchStatusesAsync(
        IReadOnlyCollection<MetaCatalogMappingResult> requestedItems,
        IReadOnlyList<string> handles,
        CancellationToken cancellationToken)
    {
        var requestedItemsByIndex = requestedItems.ToList();
        var allResults = new List<MetaCatalogItemResult>();

        for (var index = 0; index < handles.Count; index++)
        {
            IReadOnlyCollection<MetaCatalogMappingResult> itemsForHandle = handles.Count == requestedItemsByIndex.Count
                ? [requestedItemsByIndex[index]]
                : requestedItemsByIndex;
            var result = await PollBatchStatusAsync(itemsForHandle, handles[index], cancellationToken);
            allResults.AddRange(result.Items);
        }

        return new MetaCatalogBatchResult(allResults
            .GroupBy(i => i.RetailerId, StringComparer.Ordinal)
            .Select(g => g.FirstOrDefault(i => !i.Success) ?? g.First())
            .ToList());
    }

    private async Task<(bool IsTerminal, MetaCatalogBatchResult Result)> GetBatchStatusAsync(
        IReadOnlyCollection<MetaCatalogMappingResult> requestedItems,
        string handle,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds)));

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildCheckBatchStatusUrl(handle));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

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

        using var _ = response;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw BuildHttpException(
                response.StatusCode,
                body,
                response.Content.Headers.ContentType?.ToString(),
                _options.AccessToken);
        }

        MetaCatalogBatchStatusResponse? parsed;
        try
        {
            parsed = string.IsNullOrWhiteSpace(body)
                ? new MetaCatalogBatchStatusResponse()
                : JsonSerializer.Deserialize<MetaCatalogBatchStatusResponse>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new MetaCatalogApiException(
                HttpStatusCode.OK,
                "Meta Catalog returned an invalid JSON response.",
                false,
                innerException: ex);
        }

        var statusItem = parsed?.Data?.FirstOrDefault();
        var status = statusItem?.Status ?? parsed?.Status;
        var normalizedStatus = status?.Trim().ToLowerInvariant();
        var validations = statusItem?.Errors ?? parsed?.Errors;
        var validationResult = BuildValidationResult(requestedItems, validations, handle, status);
        if (validationResult != null)
        {
            return (true, validationResult);
        }

        if (IsCompletedStatus(normalizedStatus))
        {
            var warnings = (statusItem?.Warnings ?? parsed?.Warnings)?
                .SelectMany(w => w.Warnings ?? Enumerable.Empty<MetaCatalogBatchItemWarning>())
                .Select(w => w.Message)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Cast<string>()
                .ToList();

            return (true, new MetaCatalogBatchResult(requestedItems.Select(i => new MetaCatalogItemResult(
                i.RetailerId,
                i.Action,
                true,
                null,
                null,
                null,
                false,
                status,
                null,
                warnings,
                handle)).ToList()));
        }

        return (false, new MetaCatalogBatchResult(Array.Empty<MetaCatalogItemResult>()));
    }

    private static bool IsCompletedStatus(string? normalizedStatus)
    {
        return normalizedStatus is "finished" or "completed" or "complete" or "done" or "success" or "succeeded";
    }

    private MetaCatalogApiException BuildHttpException(
        HttpStatusCode statusCode,
        string body,
        string? contentType,
        string accessToken)
    {
        MetaCatalogError? error = null;
        try
        {
            error = JsonSerializer.Deserialize<MetaCatalogErrorResponse>(body, JsonOptions)?.Error;
        }
        catch (JsonException)
        {
            _logger.LogWarning(
                "Meta Catalog returned a non-JSON error response. StatusCode: {StatusCode}. BodyLength: {BodyLength}. ContentType: {ContentType}",
                (int)statusCode,
                body?.Length ?? 0,
                contentType ?? string.Empty);
        }

        var isTransient = error?.IsTransient
            ?? (statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500);
        var safeMessage = error != null
            ? SanitizeMetaMessage(error.ErrorUserMessage ?? error.Message, accessToken)
            : null;

        if (string.IsNullOrWhiteSpace(safeMessage))
        {
            safeMessage = statusCode switch
            {
                HttpStatusCode.BadRequest => "Meta Catalog rejected the request payload.",
                HttpStatusCode.Unauthorized => "Meta Catalog authentication failed.",
                HttpStatusCode.Forbidden => "Meta Catalog authorization failed.",
                HttpStatusCode.TooManyRequests => "Meta Catalog rate limit exceeded.",
                _ when (int)statusCode >= 500 => "Meta Catalog returned a transient server error.",
                _ => "Meta Catalog request failed."
            };
        }

        return new MetaCatalogApiException(
            statusCode,
            safeMessage,
            isTransient,
            error?.Code?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            error?.ErrorSubcode?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            error?.FbTraceId);
    }

    private static string? SanitizeMetaMessage(string? message, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var sanitized = message.Trim();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            sanitized = sanitized.Replace(accessToken, "[redacted]", StringComparison.Ordinal);
        }

        return sanitized
            .Replace("Authorization", "[redacted-header]", StringComparison.OrdinalIgnoreCase)
            .Replace("Bearer", "[redacted-auth-scheme]", StringComparison.OrdinalIgnoreCase);
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
