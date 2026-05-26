using System.Net;
using System.Net.Http;
using System.Text;
using Hivehook;
using Hivehook.Exceptions;
using Xunit;

namespace Hivehook.Tests;

public class TransportTest
{
    private sealed class StubHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _factory;
        public int Calls;
        public StubHandler(Func<HttpRequestMessage, int, HttpResponseMessage> factory)
        {
            _factory = factory;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var n = Interlocked.Increment(ref Calls);
            return Task.FromResult(_factory(request, n));
        }
    }

    private static HttpResponseMessage JsonResp(HttpStatusCode code, string body)
    {
        return new HttpResponseMessage(code)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    private static GraphQLTransport NewTransport(StubHandler handler, int maxRetries = 2, TimeSpan? timeout = null)
    {
        // Build the HttpClient with the stub handler at the bottom of the chain.
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://stub.local") };
        return new GraphQLTransport("http://stub.local", apiKey: null, httpClient: http, maxRetries: maxRetries, timeout: timeout ?? TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task NotFoundFromGraphQLErrorsDispatchesTyped()
    {
        var handler = new StubHandler((_, _) => JsonResp(
            HttpStatusCode.OK,
            """{"errors":[{"message":"missing","extensions":{"code":"NOT_FOUND","detail":"x"}}]}"""));
        var t = NewTransport(handler);
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => t.ExecuteAsync("query{}"));
        Assert.Equal("missing", ex.Message);
        Assert.NotNull(ex.Extensions);
        Assert.True(ex.Extensions!.ContainsKey("code"));
        Assert.True(ex.Extensions!.ContainsKey("detail"));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Http400WithValidationCodeDispatchesValidation()
    {
        var handler = new StubHandler((_, _) => JsonResp(
            HttpStatusCode.BadRequest,
            """{"errors":[{"message":"bad field","extensions":{"code":"VALIDATION","field":"name"}}]}"""));
        var t = NewTransport(handler);
        var ex = await Assert.ThrowsAsync<ValidationException>(() => t.ExecuteAsync("query{}"));
        Assert.Equal("bad field", ex.Message);
        Assert.Equal(400, ex.StatusCode);
        Assert.NotNull(ex.Extensions);
        Assert.True(ex.Extensions!.ContainsKey("field"));
    }

    [Fact]
    public async Task Http400WithConflictCodeDispatchesConflict()
    {
        var handler = new StubHandler((_, _) => JsonResp(
            HttpStatusCode.BadRequest,
            """{"errors":[{"message":"dup","extensions":{"code":"CONFLICT"}}]}"""));
        var t = NewTransport(handler);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => t.ExecuteAsync("query{}"));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UnauthorizedThrowsAuthException()
    {
        var handler = new StubHandler((_, _) => JsonResp(HttpStatusCode.Unauthorized, """{"message":"nope"}"""));
        var t = NewTransport(handler);
        var ex = await Assert.ThrowsAsync<AuthException>(() => t.ExecuteAsync("query{}"));
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal(1, handler.Calls); // never retried
    }

    [Fact]
    public async Task RateLimitWithRetryAfterSucceedsAfterRetry()
    {
        var handler = new StubHandler((_, n) =>
        {
            if (n == 1)
            {
                var r = JsonResp((HttpStatusCode)429, """{"errors":[{"message":"slow down"}]}""");
                r.Headers.TryAddWithoutValidation("Retry-After", "0");
                return r;
            }
            return JsonResp(HttpStatusCode.OK, """{"data":{"ok":true}}""");
        });
        var t = NewTransport(handler);
        var data = await t.ExecuteAsync("query{}");
        Assert.True(data.ContainsKey("ok"));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task RateLimitExhaustedThrowsRateLimitException()
    {
        var handler = new StubHandler((_, _) =>
        {
            var r = JsonResp((HttpStatusCode)429, """{"errors":[{"message":"slow"}]}""");
            r.Headers.TryAddWithoutValidation("Retry-After", "0");
            return r;
        });
        var t = NewTransport(handler, maxRetries: 1);
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => t.ExecuteAsync("query{}"));
        Assert.Equal(429, ex.StatusCode);
        Assert.NotNull(ex.RetryAfter);
        Assert.Equal(TimeSpan.Zero, ex.RetryAfter);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task ServerError5xxRetriesThenSucceeds()
    {
        var handler = new StubHandler((_, n) => n == 1
            ? JsonResp(HttpStatusCode.InternalServerError, """{"errors":[{"message":"boom"}]}""")
            : JsonResp(HttpStatusCode.OK, """{"data":{"ok":true}}"""));
        var t = NewTransport(handler);
        var data = await t.ExecuteAsync("query{}");
        Assert.True(data.ContainsKey("ok"));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task ServerError5xxExhaustsRetriesThrowsServerException()
    {
        var handler = new StubHandler((_, _) => JsonResp(HttpStatusCode.BadGateway, """{"errors":[{"message":"down"}]}"""));
        var t = NewTransport(handler, maxRetries: 1);
        var ex = await Assert.ThrowsAsync<ServerException>(() => t.ExecuteAsync("query{}"));
        Assert.Equal(502, ex.StatusCode);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task HttpRequestExceptionIsRetried()
    {
        var handler = new StubHandler((_, n) =>
        {
            if (n == 1) throw new HttpRequestException("connection reset");
            return JsonResp(HttpStatusCode.OK, """{"data":{"ok":true}}""");
        });
        var t = NewTransport(handler);
        var data = await t.ExecuteAsync("query{}");
        Assert.True(data.ContainsKey("ok"));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task ValidationFromTopLevelErrorsIsNotRetried()
    {
        var handler = new StubHandler((_, _) => JsonResp(
            HttpStatusCode.OK,
            """{"errors":[{"message":"bad","extensions":{"code":"VALIDATION"}}]}"""));
        var t = NewTransport(handler);
        await Assert.ThrowsAsync<ValidationException>(() => t.ExecuteAsync("query{}"));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ConflictFromTopLevelErrorsIsNotRetried()
    {
        var handler = new StubHandler((_, _) => JsonResp(
            HttpStatusCode.OK,
            """{"errors":[{"message":"dup","extensions":{"code":"CONFLICT"}}]}"""));
        var t = NewTransport(handler);
        await Assert.ThrowsAsync<ConflictException>(() => t.ExecuteAsync("query{}"));
        Assert.Equal(1, handler.Calls);
    }
}
