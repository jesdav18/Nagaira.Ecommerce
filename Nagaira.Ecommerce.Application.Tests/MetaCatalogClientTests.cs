using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nagaira.Ecommerce.Application.MetaCatalog;
using Nagaira.Ecommerce.Infrastructure.Integrations.MetaCatalog;

namespace Nagaira.Ecommerce.Application.Tests;

public class MetaCatalogClientTests
{
    [Fact]
    public async Task Submit_BuildsExpectedUrlWithVersionAndCatalogId()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("""{"responses":[{"retailer_id":"p-1","success":true,"id":"meta-1"}]}"""));
        var client = CreateClient(handler);

        await client.SubmitAsync([CreateUpsert("p-1")]);

        Assert.Single(handler.Requests);
        Assert.Equal("https://graph.facebook.com/v25.0/catalog-123/items_batch", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task Submit_SendsBearerAuthenticationWithoutPuttingTokenInUrl()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("""{"responses":[{"retailer_id":"p-1","success":true}]}"""));
        var client = CreateClient(handler, accessToken: "secret-token");

        await client.SubmitAsync([CreateUpsert("p-1")]);

        var request = handler.Requests[0];
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("secret-token", request.Headers.Authorization?.Parameter);
        Assert.DoesNotContain("secret-token", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task Submit_SerializesUpsertWithCatalogFields()
    {
        JsonDocument? document = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            document = JsonDocument.Parse(request.Content!.ReadAsStringAsync().Result);
            return JsonResponse("""{"responses":[{"retailer_id":"p-1","success":true}]}""");
        });
        var client = CreateClient(handler);

        await client.SubmitAsync([CreateUpsert("p-1")]);

        Assert.Equal("PRODUCT_ITEM", document!.RootElement.GetProperty("item_type").GetString());
        var requestItem = document.RootElement.GetProperty("requests")[0];
        Assert.Equal("UPDATE", requestItem.GetProperty("method").GetString());
        Assert.Equal("p-1", requestItem.GetProperty("retailer_id").GetString());
        Assert.False(requestItem.TryGetProperty("item_type", out _));
        var data = requestItem.GetProperty("data");
        Assert.Equal("Router WiFi", data.GetProperty("name").GetString());
        Assert.Equal("Acme", data.GetProperty("brand").GetString());
        Assert.Equal("125.50", data.GetProperty("price").GetString());
        Assert.Equal("HNL", data.GetProperty("currency").GetString());
        Assert.Equal("in stock", data.GetProperty("availability").GetString());
        Assert.Equal("https://store.example/p/router", data.GetProperty("link").GetString());
        Assert.Equal("https://cdn.example/router.jpg", data.GetProperty("image_link").GetString());
    }

    [Fact]
    public async Task Submit_SerializesDeleteWithoutData()
    {
        JsonDocument? document = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            document = JsonDocument.Parse(request.Content!.ReadAsStringAsync().Result);
            return JsonResponse("""{"responses":[{"retailer_id":"p-1","success":true}]}""");
        });
        var client = CreateClient(handler);

        await client.SubmitAsync([CreateDelete("p-1")]);

        Assert.Equal("PRODUCT_ITEM", document!.RootElement.GetProperty("item_type").GetString());
        var requestItem = document.RootElement.GetProperty("requests")[0];
        Assert.Equal("DELETE", requestItem.GetProperty("method").GetString());
        Assert.Equal("p-1", requestItem.GetProperty("retailer_id").GetString());
        Assert.False(requestItem.TryGetProperty("item_type", out _));
        Assert.False(requestItem.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task Submit_PreservesRetailerIdForMixedActions()
    {
        JsonDocument? document = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            document = JsonDocument.Parse(request.Content!.ReadAsStringAsync().Result);
            return JsonResponse("""{"responses":[{"retailer_id":"p-1","success":true},{"retailer_id":"p-2","success":true}]}""");
        });
        var client = CreateClient(handler);

        await client.SubmitAsync([CreateUpsert("p-1"), CreateDelete("p-2")]);

        var requests = document!.RootElement.GetProperty("requests");
        Assert.Equal("p-1", requests[0].GetProperty("retailer_id").GetString());
        Assert.Equal("p-2", requests[1].GetProperty("retailer_id").GetString());
    }

    [Fact]
    public async Task Submit_SplitsBatchesByConfiguredBatchSize()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("""{"responses":[]}"""));
        var client = CreateClient(handler, batchSize: 2);

        await client.SubmitAsync([CreateUpsert("p-1"), CreateUpsert("p-2"), CreateUpsert("p-3")]);

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Submit_EmptyBatchDoesNotCallHttp()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called"));
        var client = CreateClient(handler);

        var result = await client.SubmitAsync([]);

        Assert.Empty(result.Items);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Submit_SyncDisabledDoesNotCallHttpAndAllowsEmptyConfiguration()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called"));
        var client = CreateClient(handler, syncEnabled: false, catalogId: "", accessToken: "", graphApiVersion: "");

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        Assert.Empty(handler.Requests);
        Assert.Single(result.Items);
        Assert.True(result.Items[0].Success);
    }

    [Fact]
    public void OptionsValidator_AllowsEmptyCredentialsWhenSyncDisabled()
    {
        var validator = new MetaCatalogOptionsValidator();

        var result = validator.Validate(null, new MetaCatalogOptions { SyncEnabled = false });

        Assert.False(result.Failed);
    }

    [Fact]
    public async Task Submit_SuccessResponseMapsPerProductResult()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("""{"responses":[{"retailer_id":"p-1","success":true,"id":"meta-1"}]}"""));
        var client = CreateClient(handler);

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        Assert.False(result.HasErrors);
        Assert.Equal("p-1", result.Items[0].RetailerId);
        Assert.Equal("meta-1", result.Items[0].MetaItemId);
    }

    [Fact]
    public async Task Submit_PartialSuccessKeepsPerProductErrors()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("""
        {"responses":[
          {"retailer_id":"p-1","success":true,"id":"meta-1"},
          {"retailer_id":"p-2","success":false,"error":{"code":"400","message":"Invalid item","is_transient":false}}
        ]}
        """));
        var client = CreateClient(handler);

        var result = await client.SubmitAsync([CreateUpsert("p-1"), CreateDelete("p-2")]);

        Assert.True(result.HasErrors);
        Assert.True(result.Items.Single(i => i.RetailerId == "p-1").Success);
        var failed = result.Items.Single(i => i.RetailerId == "p-2");
        Assert.False(failed.Success);
        Assert.Equal("400", failed.ErrorCode);
        Assert.Equal("Invalid item", failed.ErrorMessage);
        Assert.False(failed.IsTransient);
        Assert.Equal(MetaCatalogSyncAction.Delete, failed.Action);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    public async Task Submit_ClassifiesHttpErrors(HttpStatusCode statusCode, bool expectedTransient)
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(
            """{"error":{"message":"Unsafe details","code":123,"error_subcode":456,"fbtrace_id":"trace-1"}}""",
            statusCode));
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<MetaCatalogApiException>(() => client.SubmitAsync([CreateUpsert("p-1")]));

        Assert.Equal(statusCode, ex.HttpStatusCode);
        Assert.Equal(expectedTransient, ex.IsTransient);
        Assert.Equal("123", ex.MetaErrorCode);
        Assert.Equal("456", ex.MetaErrorSubcode);
        Assert.Equal("trace-1", ex.RequestId);
        Assert.DoesNotContain("secret-token", ex.Message);
    }

    [Fact]
    public async Task Submit_GraphApiCode100ReturnsRealSanitizedMetaMessage()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(
            """{"error":{"message":"Invalid parameter: requests","type":"OAuthException","code":100,"is_transient":false,"fbtrace_id":"trace-100"}}""",
            HttpStatusCode.BadRequest));
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<MetaCatalogApiException>(() => client.SubmitAsync([CreateUpsert("p-1")]));

        Assert.Equal(HttpStatusCode.BadRequest, ex.HttpStatusCode);
        Assert.Equal("100", ex.MetaErrorCode);
        Assert.False(ex.IsTransient);
        Assert.Equal("Invalid parameter: requests", ex.SafeMessage);
        Assert.Equal("trace-100", ex.RequestId);
    }

    [Fact]
    public async Task Submit_GraphApiErrorSubcodeIsPreserved()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(
            """{"error":{"message":"Invalid catalog item","code":100,"error_subcode":1885316,"fbtrace_id":"trace-sub"}}""",
            HttpStatusCode.BadRequest));
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<MetaCatalogApiException>(() => client.SubmitAsync([CreateUpsert("p-1")]));

        Assert.Equal("100", ex.MetaErrorCode);
        Assert.Equal("1885316", ex.MetaErrorSubcode);
        Assert.Equal("trace-sub", ex.RequestId);
    }

    [Fact]
    public async Task Submit_GraphApiErrorUserMessageIsPreferred()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(
            """{"error":{"message":"Internal developer message","error_user_msg":"User-safe Meta message","code":100,"fbtrace_id":"trace-user"}}""",
            HttpStatusCode.BadRequest));
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<MetaCatalogApiException>(() => client.SubmitAsync([CreateUpsert("p-1")]));

        Assert.Equal("User-safe Meta message", ex.SafeMessage);
        Assert.DoesNotContain("Internal developer message", ex.SafeMessage);
    }

    [Fact]
    public async Task Submit_NonJsonErrorResponseReturnsGenericMessage()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("<html>bad request</html>")
        });
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<MetaCatalogApiException>(() => client.SubmitAsync([CreateUpsert("p-1")]));

        Assert.Equal(HttpStatusCode.BadRequest, ex.HttpStatusCode);
        Assert.Equal("Meta Catalog rejected the request payload.", ex.SafeMessage);
        Assert.Null(ex.MetaErrorCode);
        Assert.Null(ex.MetaErrorSubcode);
    }

    [Fact]
    public async Task Submit_GraphApiMessageNeverReturnsAccessToken()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(
            """{"error":{"message":"Token secret-token is invalid in Authorization Bearer header","code":100,"fbtrace_id":"trace-token"}}""",
            HttpStatusCode.BadRequest));
        var client = CreateClient(handler, accessToken: "secret-token");

        var ex = await Assert.ThrowsAsync<MetaCatalogApiException>(() => client.SubmitAsync([CreateUpsert("p-1")]));

        Assert.DoesNotContain("secret-token", ex.SafeMessage);
        Assert.DoesNotContain("Authorization", ex.SafeMessage);
        Assert.DoesNotContain("Bearer", ex.SafeMessage);
        Assert.Contains("[redacted]", ex.SafeMessage);
    }

    [Fact]
    public async Task Submit_TimeoutIsTransient()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new TimeoutException());
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<MetaCatalogApiException>(() => client.SubmitAsync([CreateUpsert("p-1")]));

        Assert.True(ex.IsTransient);
        Assert.Contains("timed out", ex.SafeMessage);
    }

    [Fact]
    public async Task Submit_CallerCancellationIsPropagated()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new FakeHttpMessageHandler(_ => throw new OperationCanceledException(cts.Token));
        var client = CreateClient(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.SubmitAsync([CreateUpsert("p-1")], cts.Token));
    }

    [Fact]
    public async Task Submit_InvalidJsonResponseThrowsControlledException()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ invalid")
        });
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<MetaCatalogApiException>(() => client.SubmitAsync([CreateUpsert("p-1")]));

        Assert.False(ex.IsTransient);
        Assert.Contains("invalid JSON", ex.SafeMessage);
        Assert.DoesNotContain("secret-token", ex.Message);
    }

    [Fact]
    public async Task Submit_BatchHandleAcceptedAndApplied_ReturnsSuccessAfterStatusPolling()
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                return JsonResponse("""{"handles":["abc123"]}""");
            }

            return JsonResponse("""{"data":[{"handle":"abc123","status":"finished","errors_total_count":0,"warnings_total_count":0}]}""");
        });
        var client = CreateClient(handler, delay: (_, _) => Task.CompletedTask);

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        var item = Assert.Single(result.Items);
        Assert.True(item.Success);
        Assert.Equal("finished", item.Status);
        Assert.Equal("abc123", item.BatchHandle);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Contains("/check_batch_request_status", handler.Requests[1].RequestUri!.ToString());
        Assert.Contains("handle=abc123", handler.Requests[1].RequestUri!.ToString());
        Assert.Contains("fields=handle%2Cstatus%2Cerrors%2Cerrors_total_count%2Cwarnings%2Cwarnings_total_count%2Cids_of_invalid_requests", handler.Requests[1].RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task Submit_BatchHandleStillProcessingAfterPolling_ReturnsTransientFailure()
    {
        var delayCount = 0;
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Post
                ? JsonResponse("""{"handles":["batch-handle-1"]}""")
                : JsonResponse("""{"data":[{"status":"processing"}]}"""));
        var client = CreateClient(handler, delay: (_, _) =>
        {
            delayCount++;
            return Task.CompletedTask;
        });

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        var item = Assert.Single(result.Items);
        Assert.False(item.Success);
        Assert.True(item.IsTransient);
        Assert.Equal("processing", item.Status);
        Assert.Equal("Meta Catalog batch is still processing.", item.ErrorMessage);
        Assert.Equal("batch-handle-1", item.BatchHandle);
        Assert.Equal(6, handler.Requests.Count);
        Assert.Equal(4, delayCount);
    }

    [Fact]
    public async Task Submit_MultipleBatchHandles_PollsEachHandle()
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                return JsonResponse("""{"handles":["handle-1","handle-2"]}""");
            }

            var handle = request.RequestUri!.Query.Contains("handle-1", StringComparison.Ordinal)
                ? "handle-1"
                : "handle-2";
            return JsonResponse($$"""{"data":[{"handle":"{{handle}}","status":"finished","errors_total_count":0}]}""");
        });
        var client = CreateClient(handler, delay: (_, _) => Task.CompletedTask);

        var result = await client.SubmitAsync([CreateUpsert("p-1"), CreateDelete("p-2")]);

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items.All(i => i.Success));
        Assert.Equal("handle-1", result.Items.Single(i => i.RetailerId == "p-1").BatchHandle);
        Assert.Equal("handle-2", result.Items.Single(i => i.RetailerId == "p-2").BatchHandle);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Contains("handle=handle-1", handler.Requests[1].RequestUri!.ToString());
        Assert.Contains("handle=handle-2", handler.Requests[2].RequestUri!.ToString());
    }

    [Fact]
    public async Task Submit_BatchHandleWithIndividualError_ReturnsSanitizedPerItemError()
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                return JsonResponse("""{"handles":["batch-handle-1"]}""");
            }

            return JsonResponse("""
            {"data":[{"status":"finished","errors":[{"retailer_id":"p-1","errors":[{"code":"100","error_subcode":"1885316","message":"Invalid product secret-token Authorization Bearer","is_transient":false}],"warnings":[{"message":"Image warning"}]}]}]}
            """);
        });
        var client = CreateClient(handler, accessToken: "secret-token", delay: (_, _) => Task.CompletedTask);

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        var item = Assert.Single(result.Items);
        Assert.False(item.Success);
        Assert.Equal("finished", item.Status);
        Assert.Equal("100", item.ErrorCode);
        Assert.Equal("1885316", item.ErrorSubcode);
        Assert.Equal("batch-handle-1", item.BatchHandle);
        Assert.Contains("Image warning", item.Warnings!);
        Assert.DoesNotContain("secret-token", item.ErrorMessage);
        Assert.DoesNotContain("Authorization", item.ErrorMessage);
        Assert.DoesNotContain("Bearer", item.ErrorMessage);
        Assert.Contains("[redacted]", item.ErrorMessage);
    }

    [Fact]
    public async Task Submit_BatchStatusWithErrorCount_ReturnsFailure()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Post
                ? JsonResponse("""{"handles":["batch-handle-1"]}""")
                : JsonResponse("""{"data":[{"handle":"batch-handle-1","status":"finished","errors_total_count":1,"ids_of_invalid_requests":["0"]}]}"""));
        var client = CreateClient(handler, delay: (_, _) => Task.CompletedTask);

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        var item = Assert.Single(result.Items);
        Assert.False(item.Success);
        Assert.False(item.IsTransient);
        Assert.Equal("finished", item.Status);
        Assert.Equal("batch-handle-1", item.BatchHandle);
        Assert.Contains("completed with errors", item.ErrorMessage);
    }

    [Fact]
    public async Task Submit_EmptyHandlesArray_ReturnsTransientFailureWithoutPolling()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("""{"handles":[]}"""));
        var client = CreateClient(handler);

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        var item = Assert.Single(result.Items);
        Assert.False(item.Success);
        Assert.True(item.IsTransient);
        Assert.Equal("missing_handle", item.Status);
        Assert.Equal("Meta Catalog batch response did not include a handle.", item.ErrorMessage);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Submit_FirstHandleEmpty_ReturnsTransientFailureWithoutUsingLaterHandles()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("""{"handles":["","abc123"]}"""));
        var client = CreateClient(handler);

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        var item = Assert.Single(result.Items);
        Assert.False(item.Success);
        Assert.True(item.IsTransient);
        Assert.Equal("missing_handle", item.Status);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Submit_MissingHandlesProperty_ReturnsTransientFailureWithoutPolling()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("""{}"""));
        var client = CreateClient(handler);

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        var item = Assert.Single(result.Items);
        Assert.False(item.Success);
        Assert.True(item.IsTransient);
        Assert.Equal("missing_handle", item.Status);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Submit_SingularHandleCompatibility_PollsHandle()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Post
                ? JsonResponse("""{"handle":"abc123"}""")
                : JsonResponse("""{"data":[{"handle":"abc123","status":"finished","errors_total_count":0}]}"""));
        var client = CreateClient(handler, delay: (_, _) => Task.CompletedTask);

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        var item = Assert.Single(result.Items);
        Assert.True(item.Success);
        Assert.Equal("abc123", item.BatchHandle);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("handle=abc123", handler.Requests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task Submit_MissingHandleIncludesSanitizedDiagnosticBody()
    {
        const string accessToken = "secret-token";
        var body = """
        {"unexpected":true,"access_token":"secret-token","Authorization":"Bearer abc123","message":"secret-token Authorization Bearer abc123"}
        """;
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(body, contentType: "application/json; charset=utf-8"));
        var client = CreateClient(handler, accessToken: accessToken);

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        var item = Assert.Single(result.Items);
        Assert.False(item.Success);
        Assert.Equal("missing_handle", item.Status);
        Assert.Equal("application/json; charset=utf-8", item.ResponseContentType);
        Assert.Equal(body.Length, item.ResponseBodyLength);
        Assert.Contains("unexpected", item.ResponseTopLevelProperties!);
        Assert.Contains("[redacted]", item.ResponseTopLevelProperties!);
        Assert.DoesNotContain("access_token", item.DiagnosticResponseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(accessToken, item.DiagnosticResponseBody);
        Assert.DoesNotContain("Authorization", item.DiagnosticResponseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer", item.DiagnosticResponseBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[redacted]", item.DiagnosticResponseBody);
    }

    [Fact]
    public async Task Submit_MissingHandleLimitsDiagnosticBodyToTwoThousandCharacters()
    {
        var longValue = new string('x', 3000);
        var body = $$"""{"unexpected":"{{longValue}}"}""";
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(body));
        var client = CreateClient(handler);

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        var item = Assert.Single(result.Items);
        Assert.False(item.Success);
        Assert.Equal(2000, item.DiagnosticResponseBody!.Length);
        Assert.Equal(body.Length, item.ResponseBodyLength);
    }

    [Fact]
    public async Task Submit_BatchStatusResultNeverReturnsAccessToken()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Post
                ? JsonResponse("""{"handle":"batch-handle-1"}""")
                : JsonResponse("""
                {"data":[{"status":"finished","errors":[{"retailer_id":"p-1","errors":[{"code":"100","message":"secret-token should not appear","is_transient":false}]}]}]}
                """));
        var client = CreateClient(handler, accessToken: "secret-token", delay: (_, _) => Task.CompletedTask);

        var result = await client.SubmitAsync([CreateUpsert("p-1")]);

        var item = Assert.Single(result.Items);
        Assert.False(item.Success);
        Assert.DoesNotContain("secret-token", JsonSerializer.Serialize(result));
        Assert.DoesNotContain("secret-token", item.ErrorMessage);
    }

    private static MetaCatalogClient CreateClient(
        FakeHttpMessageHandler handler,
        bool syncEnabled = true,
        int batchSize = 100,
        string catalogId = "catalog-123",
        string accessToken = "secret-token",
        string graphApiVersion = "v25.0",
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        var httpClient = new HttpClient(handler);
        var factory = new FakeHttpClientFactory(httpClient);
        var options = Options.Create(new MetaCatalogOptions
        {
            ApiBaseUrl = "https://graph.facebook.com",
            GraphApiVersion = graphApiVersion,
            CatalogId = catalogId,
            AccessToken = accessToken,
            SyncEnabled = syncEnabled,
            BatchSize = batchSize,
            RequestTimeoutSeconds = 30
        });

        return new MetaCatalogClient(factory, options, NullLogger<MetaCatalogClient>.Instance, delay);
    }

    private static MetaCatalogMappingResult CreateUpsert(string retailerId)
    {
        var item = new MetaCatalogProduct(
            retailerId,
            "Router WiFi",
            "Router para casa",
            "Acme",
            "in stock",
            "new",
            "125.50",
            "HNL",
            "https://store.example/p/router",
            "https://cdn.example/router.jpg",
            "Redes",
            "RTR-001");

        return new MetaCatalogMappingResult(
            MetaCatalogSyncAction.Upsert,
            retailerId,
            item,
            "hash");
    }

    private static MetaCatalogMappingResult CreateDelete(string retailerId)
    {
        return new MetaCatalogMappingResult(
            MetaCatalogSyncAction.Delete,
            retailerId,
            null,
            "hash");
    }

    private static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string? contentType = null)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json)
        };
        if (contentType != null)
        {
            response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
        }

        return response;
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FakeHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_handler(request));
        }
    }
}
