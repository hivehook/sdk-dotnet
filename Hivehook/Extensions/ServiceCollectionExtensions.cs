using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Hivehook.Extensions;

/// <summary>
/// Strongly-typed options for the Hivehook DI integration.
/// </summary>
public sealed record HivehookClientOptions
{
    /// <summary>Base URL of the Hivehook server (without the trailing <c>/graphql</c> suffix).</summary>
    public string BaseUrl { get; init; } = "http://localhost:8080";

    /// <summary>Optional API key sent as a Bearer token on every request.</summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Maximum number of retries for transient failures (HTTP 429, 5xx, network errors).
    /// Retries honor the server's <c>Retry-After</c> header when present.
    /// </summary>
    public int MaxRetries { get; init; } = 2;

    /// <summary>
    /// Per-request timeout applied via a linked <see cref="System.Threading.CancellationTokenSource"/>.
    /// Use <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to disable.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// <see cref="IServiceCollection"/> extensions for registering <see cref="HivehookClient"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>The name used for the typed <see cref="HttpClient"/> registered by <see cref="AddHivehook"/>.</summary>
    public const string HttpClientName = "Hivehook";

    /// <summary>
    /// Register <see cref="HivehookClient"/> as a typed client backed by
    /// <see cref="IHttpClientFactory"/>. The factory rotates the underlying
    /// <see cref="HttpMessageHandler"/> to refresh DNS and reuse sockets correctly.
    /// </summary>
    /// <remarks>
    /// <b>Lifetime change:</b> Earlier releases registered <see cref="HivehookClient"/> as a
    /// singleton. With the typed-client pattern the client is <i>transient</i> (the default
    /// for <c>AddHttpClient&lt;T&gt;</c>). DI hands out a fresh instance each scope while
    /// the message handler is pooled and reused. Callers that previously stored the client
    /// in a static field should resolve it from DI each request instead. Do not call
    /// <see cref="HivehookClient.Dispose"/> on instances resolved via DI; the container owns
    /// the lifetime and disposing would release the pooled handler.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Callback that fills in <see cref="HivehookClientOptions"/>.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> so callers can attach handlers (e.g. Polly).</returns>
    public static IHttpClientBuilder AddHivehook(this IServiceCollection services, Action<HivehookClientOptions> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new HivehookClientOptions();
        configure(options);

        services.AddSingleton(options);

        return services.AddHttpClient<HivehookClient>(http =>
        {
            // Leave HttpClient.Timeout at its infinite default and enforce the timeout
            // inside the transport via a linked CTS, so per-call cancellation
            // tokens behave consistently and retries get a fresh budget each attempt.
            http.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
            http.DefaultRequestHeaders.UserAgent.ParseAdd("hivehook-dotnet/" + GraphQLTransport.Version);
            if (!string.IsNullOrEmpty(options.ApiKey))
            {
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            }
        });
    }
}
