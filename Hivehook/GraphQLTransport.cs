using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Exceptions;

namespace Hivehook;

/// <summary>
/// Thin GraphQL-over-HTTP transport used by all Hivehook resource services.
/// </summary>
public class GraphQLTransport
{
    /// <summary>The Hivehook .NET SDK version, sent in the User-Agent header.</summary>
    public const string Version = "0.1.0";
    private const string UserAgent = "hivehook-dotnet/" + Version;

    /// <summary>
    /// Shared <see cref="JsonSerializerOptions"/> used by resource services to deserialize
    /// GraphQL response slices into typed records.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    private readonly HttpClient _httpClient;
    private readonly string _graphqlUrl;
    private readonly int _maxRetries;
    private readonly TimeSpan _timeout;

    /// <summary>
    /// Initialize a transport pointing at the Hivehook GraphQL endpoint.
    /// </summary>
    /// <param name="baseUrl">Server base URL (without the /graphql suffix).</param>
    /// <param name="apiKey">Optional API key sent as a Bearer token.</param>
    /// <param name="httpClient">Optional caller-supplied <see cref="HttpClient"/>.</param>
    /// <param name="maxRetries">Maximum number of retries for transient failures (429, 5xx, network).</param>
    /// <param name="timeout">Per-request timeout. Use <see cref="Timeout.InfiniteTimeSpan"/> to disable.</param>
    public GraphQLTransport(
        string baseUrl,
        string? apiKey = null,
        HttpClient? httpClient = null,
        int maxRetries = 2,
        TimeSpan? timeout = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _graphqlUrl = baseUrl.TrimEnd('/') + "/graphql";
        _maxRetries = Math.Max(0, maxRetries);
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
        if (apiKey != null && _httpClient.DefaultRequestHeaders.Authorization == null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }
    }

    /// <summary>
    /// Execute a GraphQL query or mutation and return the top-level <c>data</c> fields
    /// as a dictionary of <see cref="JsonElement"/>. Resource services use this to grab
    /// a single field and deserialize their typed slice.
    /// </summary>
    /// <param name="query">The GraphQL query or mutation string.</param>
    /// <param name="variables">Optional variables map.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A dictionary keyed by top-level <c>data</c> field name.</returns>
    public async Task<Dictionary<string, JsonElement>> ExecuteAsync(string query, Dictionary<string, object?>? variables = null, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?> { ["query"] = query };
        if (variables != null)
            body["variables"] = variables;

        var json = JsonSerializer.Serialize(body);

        Exception? lastError = null;
        for (var attempt = 0; attempt <= _maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Per-request timeout via a linked CTS so we do not mutate the shared HttpClient.
            using var timeoutCts = _timeout == Timeout.InfiniteTimeSpan
                ? new CancellationTokenSource()
                : new CancellationTokenSource(_timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            HttpResponseMessage? response = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _graphqlUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token).ConfigureAwait(false);

                return await HandleResponse(response, linked.Token).ConfigureAwait(false);
            }
            catch (RateLimitException rl) when (attempt < _maxRetries)
            {
                lastError = rl;
                var delay = rl.RetryAfter ?? BackoffDelay(attempt);
                await DelayAsync(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (ServerException se) when (attempt < _maxRetries)
            {
                lastError = se;
                await DelayAsync(BackoffDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException hre) when (attempt < _maxRetries && !cancellationToken.IsCancellationRequested)
            {
                lastError = hre;
                await DelayAsync(BackoffDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException tce) when (
                attempt < _maxRetries
                && !cancellationToken.IsCancellationRequested
                && timeoutCts.IsCancellationRequested)
            {
                // Per-request timeout fired; retry. (Caller-driven cancellation re-throws above.)
                lastError = tce;
                await DelayAsync(BackoffDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                response?.Dispose();
            }
        }

        // Should not reach here, but defensively rethrow the last observed error.
        throw lastError ?? new ApiException("request failed", null);
    }

    private static async Task<Dictionary<string, JsonElement>> HandleResponse(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Drain body for diagnostics but do not block on cancellation.
            var authBody = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
            throw new AuthException(ExtractMessage(authBody, "unauthorized"), 401);
        }

        var status = (int)response.StatusCode;
        var responseBody = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);

        if (status == 429)
        {
            TimeSpan? retryAfter = null;
            if (response.Headers.RetryAfter != null)
            {
                if (response.Headers.RetryAfter.Delta.HasValue)
                    retryAfter = response.Headers.RetryAfter.Delta;
                else if (response.Headers.RetryAfter.Date.HasValue)
                    retryAfter = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
            }
            else if (response.Headers.TryGetValues("Retry-After", out var values))
            {
                var raw = values.FirstOrDefault();
                if (!string.IsNullOrEmpty(raw)
                    && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var secs))
                {
                    retryAfter = TimeSpan.FromSeconds(secs);
                }
            }

            var (msg429, ext429) = ParseGraphQLError(responseBody);
            throw new RateLimitException(msg429 ?? ExtractMessage(responseBody, "rate limited"), retryAfter, 429)
            {
                Extensions = ext429,
            };
        }

        if (status >= 500)
        {
            var (msg5, ext5) = ParseGraphQLError(responseBody);
            throw new ServerException(msg5 ?? ExtractMessage(responseBody, $"server error {status}"), status)
            {
                Extensions = ext5,
            };
        }

        if (status == 400)
        {
            // Run the same NOT_FOUND/VALIDATION/CONFLICT dispatch as the 200-with-errors branch
            // so the SDK presents a consistent typed exception shape regardless of which side the
            // server chose to encode the error in.
            DispatchGraphQLErrorIfPresent(responseBody, fallbackStatus: 400);
            // Fallback when 400 had no parseable GraphQL errors envelope.
            throw new ApiException(ExtractMessage(responseBody, "bad request"), 400);
        }

        if (status >= 400)
        {
            DispatchGraphQLErrorIfPresent(responseBody, fallbackStatus: status);
            throw new ApiException(ExtractMessage(responseBody, $"http {status}"), status);
        }

        // 2xx
        var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        DispatchGraphQLErrorIfPresent(responseBody, fallbackStatus: null);

        if (!root.TryGetProperty("data", out var data))
            throw new ApiException("empty response data", 500);

        var result = new Dictionary<string, JsonElement>();
        foreach (var prop in data.EnumerateObject())
            result[prop.Name] = prop.Value.Clone();

        return result;
    }

    /// <summary>
    /// If the body parses as a GraphQL response with non-empty <c>errors</c>, throw the
    /// appropriate typed exception. Otherwise return so the caller can fall back.
    /// </summary>
    private static void DispatchGraphQLErrorIfPresent(string responseBody, int? fallbackStatus)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(responseBody);
        }
        catch (JsonException)
        {
            return;
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return;
            if (!root.TryGetProperty("errors", out var errors)) return;
            if (errors.ValueKind != JsonValueKind.Array || errors.GetArrayLength() == 0) return;

            var err = errors[0];
            var message = err.TryGetProperty("message", out var msgProp)
                ? msgProp.GetString() ?? "unknown error"
                : "unknown error";

            string? code = null;
            IReadOnlyDictionary<string, JsonElement>? extensions = null;
            if (err.TryGetProperty("extensions", out var ext) && ext.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, JsonElement>();
                foreach (var p in ext.EnumerateObject())
                    dict[p.Name] = p.Value.Clone();
                extensions = dict;
                if (ext.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.String)
                    code = codeProp.GetString();
            }

            ApiException toThrow = code switch
            {
                "NOT_FOUND" => new NotFoundException(message, fallbackStatus),
                "CONFLICT" => new ConflictException(message, fallbackStatus),
                "VALIDATION" => new ValidationException(message, fallbackStatus),
                "UNAUTHORIZED" or "UNAUTHENTICATED" => new AuthException(message, fallbackStatus ?? 401),
                _ => new ApiException(message, fallbackStatus),
            };
            throw Attach(toThrow, extensions);
        }
    }

    private static ApiException Attach(ApiException ex, IReadOnlyDictionary<string, JsonElement>? extensions)
    {
        return ex switch
        {
            NotFoundException nf => new NotFoundException(nf.Message, nf.StatusCode) { Extensions = extensions },
            ConflictException cf => new ConflictException(cf.Message, cf.StatusCode) { Extensions = extensions },
            ValidationException ve => new ValidationException(ve.Message, ve.StatusCode) { Extensions = extensions },
            AuthException au => new AuthException(au.Message, au.StatusCode) { Extensions = extensions },
            _ => new ApiException(ex.Message, ex.StatusCode) { Extensions = extensions },
        };
    }

    private static (string? Message, IReadOnlyDictionary<string, JsonElement>? Extensions) ParseGraphQLError(string body)
    {
        if (string.IsNullOrEmpty(body)) return (null, null);
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return (null, null);
            if (!root.TryGetProperty("errors", out var errors)
                || errors.ValueKind != JsonValueKind.Array
                || errors.GetArrayLength() == 0)
                return (null, null);
            var err = errors[0];
            string? msg = null;
            if (err.TryGetProperty("message", out var mp) && mp.ValueKind == JsonValueKind.String)
                msg = mp.GetString();
            IReadOnlyDictionary<string, JsonElement>? ext = null;
            if (err.TryGetProperty("extensions", out var ep) && ep.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, JsonElement>();
                foreach (var p in ep.EnumerateObject())
                    dict[p.Name] = p.Value.Clone();
                ext = dict;
            }
            return (msg, ext);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static string ExtractMessage(string body, string fallback)
    {
        if (string.IsNullOrEmpty(body)) return fallback;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("errors", out var errs)
                    && errs.ValueKind == JsonValueKind.Array
                    && errs.GetArrayLength() > 0
                    && errs[0].TryGetProperty("message", out var m1))
                    return m1.GetString() ?? fallback;
                if (root.TryGetProperty("message", out var m2))
                    return m2.GetString() ?? fallback;
            }
            return body;
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        // ReadAsStringAsync on ns2.0 ignores cancellation tokens; register dispose to abort the read.
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var registration = cancellationToken.Register(() => response.Dispose());
        _ = response.Content.ReadAsStringAsync().ContinueWith(t =>
        {
            registration.Dispose();
            if (t.IsCanceled || cancellationToken.IsCancellationRequested) tcs.TrySetCanceled(cancellationToken);
            else if (t.IsFaulted) tcs.TrySetException(t.Exception!.InnerExceptions);
            else tcs.TrySetResult(t.Result);
        }, TaskScheduler.Default);
        return tcs.Task;
#else
        return response.Content.ReadAsStringAsync(cancellationToken);
#endif
    }

    private static TimeSpan BackoffDelay(int attempt)
    {
        // 200ms, 400ms, 800ms, ... capped at 5s.
        var ms = Math.Min(5000, 200 * (1 << attempt));
        return TimeSpan.FromMilliseconds(ms);
    }

    private static Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero) return Task.CompletedTask;
        return Task.Delay(delay, cancellationToken);
    }
}
