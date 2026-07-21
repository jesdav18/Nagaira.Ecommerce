using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
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
        var requestBody = JsonSerializer.Serialize(BuildRequest(items), JsonOptions);
        request.Content = new StringContent(
            requestBody,
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

        return await ParseAcceptedBatchResponseAsync(
            items,
            body,
            response.Content.Headers.ContentType?.ToString(),
            requestBody,
            cancellationToken);
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
        var fields = Uri.EscapeDataString("handle,status,errors,errors_total_count,warnings,warnings_total_count,ids_of_invalid_requests");
        return new Uri($"{baseUrl}/{version}/{catalogId}/check_batch_request_status?handle={escapedHandle}&fields={fields}");
    }

    private static MetaCatalogBatchRequest BuildRequest(IEnumerable<MetaCatalogMappingResult> items)
    {
        return new MetaCatalogBatchRequest
        {
            Requests = items.Select(item => new MetaCatalogBatchRequestItem
            {
                Method = item.Action == MetaCatalogSyncAction.Upsert ? "CREATE" : "DELETE",
                RetailerId = item.RetailerId,
                Data = item.Action == MetaCatalogSyncAction.Upsert && item.Item != null
                    ? new MetaCatalogItemData
                    {
                        Id = item.RetailerId,
                        Name = item.Item.Name,
                        Description = item.Item.Description,
                        Brand = item.Item.Brand,
                        Availability = item.Item.Availability,
                        Condition = item.Item.Condition,
                        Price = FormatMetaPrice(item.Item),
                        Link = item.Item.Url,
                        ImageLink = item.Item.ImageUrl,
                        ProductType = item.Item.CategoryName,
                        Sku = item.Item.Sku
                    }
                    : new MetaCatalogItemData
                    {
                        Id = item.RetailerId
                    }
            }).ToList()
        };
    }

    private static string FormatMetaPrice(MetaCatalogProduct payload)
    {
        var amount = decimal.Parse(payload.Price, NumberStyles.Number, CultureInfo.InvariantCulture);
        return $"{amount.ToString("0.00", CultureInfo.InvariantCulture)} {payload.Currency.ToUpperInvariant()}";
    }

    private async Task<MetaCatalogBatchResult> ParseAcceptedBatchResponseAsync(
        IReadOnlyCollection<MetaCatalogMappingResult> requestedItems,
        string body,
        string? contentType,
        string requestBody,
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

        var validationResult = BuildValidationResult(
            requestedItems,
            parsed?.ValidationStatus,
            null,
            "validation_failed",
            requestBody);
        if (validationResult != null)
        {
            return validationResult;
        }

        var handles = GetBatchHandles(parsed);
        if (handles != null)
        {
            if (handles.Count == 0 || string.IsNullOrWhiteSpace(handles[0]))
            {
                return MissingHandleResult(requestedItems, body, contentType);
            }

            if (handles.Count == 1)
            {
                return await PollBatchStatusAsync(requestedItems, handles[0].Trim(), cancellationToken);
            }

            return await PollBatchStatusesAsync(
                requestedItems,
                handles.Select(h => h.Trim()).Where(h => !string.IsNullOrWhiteSpace(h)).ToList(),
                cancellationToken);
        }

        var directResult = BuildDirectResponseResult(requestedItems, parsed);
        if (directResult != null)
        {
            return directResult;
        }

        return MissingHandleResult(requestedItems, body, contentType);
    }

    private static List<string>? GetBatchHandles(MetaCatalogBatchResponse? parsed)
    {
        if (parsed?.Handles != null)
        {
            return parsed.Handles;
        }

        if (!string.IsNullOrWhiteSpace(parsed?.Handle))
        {
            return [parsed.Handle];
        }

        return null;
    }

    private MetaCatalogBatchResult MissingHandleResult(
        IReadOnlyCollection<MetaCatalogMappingResult> requestedItems,
        string body,
        string? contentType)
    {
        var sanitizedBody = SanitizeDiagnosticBody(body, _options.AccessToken);
        var diagnosticBody = TruncateDiagnosticBody(sanitizedBody, 2000);
        var topLevelProperties = GetSanitizedTopLevelProperties(body, _options.AccessToken);

        return new MetaCatalogBatchResult(requestedItems.Select(i => new MetaCatalogItemResult(
            i.RetailerId,
            i.Action,
            false,
            null,
            null,
            "Meta Catalog batch response did not include a handle.",
            true,
            "missing_handle",
            null,
            null,
            null,
            contentType,
            body.Length,
            topLevelProperties,
            diagnosticBody)).ToList());
    }

    private MetaCatalogBatchResult? BuildValidationResult(
        IReadOnlyCollection<MetaCatalogMappingResult> requestedItems,
        List<MetaCatalogBatchValidationStatus>? validationStatuses,
        string? batchHandle,
        string? status,
        string? requestBody = null)
    {
        if (validationStatuses == null || validationStatuses.Count == 0)
        {
            return null;
        }

        var hasAnyErrors = validationStatuses.Any(v => v.Errors?.Count > 0);
        if (!hasAnyErrors)
        {
            return null;
        }

        var validationByRetailerId = validationStatuses
            .Where(v => !string.IsNullOrWhiteSpace(v.RetailerId))
            .GroupBy(v => v.RetailerId!)
            .ToDictionary(g => g.Key, g => g.First());
        var fallbackValidation = validationStatuses.FirstOrDefault(v => v.Errors?.Count > 0);
        var diagnosticRequestBody = TruncateDiagnosticBody(SanitizeDiagnosticBody(requestBody, _options.AccessToken), 4000);

        return new MetaCatalogBatchResult(requestedItems.Select(item =>
        {
            if (!validationByRetailerId.TryGetValue(item.RetailerId, out var validation))
            {
                validation = fallbackValidation;
            }

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
                batchHandle,
                null,
                null,
                null,
                null,
                diagnosticRequestBody);
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
            return null;
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

        if (HasStatusErrors(statusItem, parsed))
        {
            return (true, BuildStatusErrorResult(requestedItems, handle, status, statusItem, parsed));
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
                statusItem?.Handle ?? parsed?.Handle ?? handle)).ToList()));
        }

        return (false, new MetaCatalogBatchResult(Array.Empty<MetaCatalogItemResult>()));
    }

    private static bool HasStatusErrors(MetaCatalogBatchStatusItem? statusItem, MetaCatalogBatchStatusResponse? parsed)
    {
        return (statusItem?.ErrorsTotalCount ?? parsed?.ErrorsTotalCount ?? 0) > 0
            || (statusItem?.IdsOfInvalidRequests?.Count ?? parsed?.IdsOfInvalidRequests?.Count ?? 0) > 0;
    }

    private MetaCatalogBatchResult BuildStatusErrorResult(
        IReadOnlyCollection<MetaCatalogMappingResult> requestedItems,
        string handle,
        string? status,
        MetaCatalogBatchStatusItem? statusItem,
        MetaCatalogBatchStatusResponse? parsed)
    {
        var warnings = (statusItem?.Warnings ?? parsed?.Warnings)?
            .SelectMany(w => w.Warnings ?? Enumerable.Empty<MetaCatalogBatchItemWarning>())
            .Select(w => w.Message)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Cast<string>()
            .ToList();
        var invalidRequestIds = statusItem?.IdsOfInvalidRequests ?? parsed?.IdsOfInvalidRequests;
        var invalidSuffix = invalidRequestIds is { Count: > 0 }
            ? $" Invalid request ids: {string.Join(",", invalidRequestIds)}."
            : string.Empty;
        var message = SanitizeMetaMessage(
            $"Meta Catalog batch completed with errors.{invalidSuffix}",
            _options.AccessToken);

        return new MetaCatalogBatchResult(requestedItems.Select(i => new MetaCatalogItemResult(
            i.RetailerId,
            i.Action,
            false,
            null,
            null,
            message,
            false,
            status,
            null,
            warnings,
            statusItem?.Handle ?? parsed?.Handle ?? handle)).ToList());
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

    private static string SanitizeDiagnosticBody(string? body, string accessToken)
    {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        var sanitized = body;
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            sanitized = sanitized.Replace(accessToken, "[redacted]", StringComparison.Ordinal);
        }

        sanitized = Regex.Replace(
            sanitized,
            "(access_token\\s*[=:]\\s*)([^\\s&\"',}]+)",
            "$1[redacted]",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        sanitized = Regex.Replace(
            sanitized,
            "(\"access_token\"\\s*:\\s*\")([^\"]*)(\")",
            "$1[redacted]$3",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        sanitized = Regex.Replace(
            sanitized,
            "\"access_token\"",
            "\"[redacted]\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        sanitized = Regex.Replace(
            sanitized,
            "(Authorization\\s*[:=]\\s*)([^\\r\\n,}]+)",
            "$1[redacted]",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        sanitized = Regex.Replace(
            sanitized,
            "\\bBearer\\s+[A-Za-z0-9._~+/=-]+",
            "[redacted-auth-scheme] [redacted]",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return sanitized
            .Replace("Authorization", "[redacted-header]", StringComparison.OrdinalIgnoreCase)
            .Replace("Bearer", "[redacted-auth-scheme]", StringComparison.OrdinalIgnoreCase);
    }

    private static string TruncateDiagnosticBody(string body, int maxLength)
    {
        return body.Length <= maxLength
            ? body
            : body[..maxLength];
    }

    private static IReadOnlyList<string> GetSanitizedTopLevelProperties(string body, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return Array.Empty<string>();
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Array.Empty<string>();
            }

            return document.RootElement
                .EnumerateObject()
                .Select(p => SanitizeDiagnosticPropertyName(p.Name, accessToken))
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static string SanitizeDiagnosticPropertyName(string propertyName, string accessToken)
    {
        if (string.Equals(propertyName, "access_token", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "authorization", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "bearer", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(accessToken) && string.Equals(propertyName, accessToken, StringComparison.Ordinal)))
        {
            return "[redacted]";
        }

        return propertyName;
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
