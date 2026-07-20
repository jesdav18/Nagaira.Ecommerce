using System.Net;
using System.Text.Json;
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

        var requestItem = document!.RootElement.GetProperty("requests")[0];
        Assert.Equal("UPDATE", requestItem.GetProperty("method").GetString());
        Assert.Equal("p-1", requestItem.GetProperty("retailer_id").GetString());
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

        var requestItem = document!.RootElement.GetProperty("requests")[0];
        Assert.Equal("DELETE", requestItem.GetProperty("method").GetString());
        Assert.Equal("p-1", requestItem.GetProperty("retailer_id").GetString());
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

    private static MetaCatalogClient CreateClient(
        FakeHttpMessageHandler handler,
        bool syncEnabled = true,
        int batchSize = 100,
        string catalogId = "catalog-123",
        string accessToken = "secret-token",
        string graphApiVersion = "v25.0")
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

        return new MetaCatalogClient(factory, options);
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

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json)
        };
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
