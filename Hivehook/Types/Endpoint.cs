using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// An outbound webhook endpoint under an Application.
/// </summary>
public sealed record Endpoint(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("applicationId")] string ApplicationId = "",
    [property: JsonPropertyName("url")] string Url = "",
    [property: JsonPropertyName("signingSecret")] string SigningSecret = "",
    [property: JsonPropertyName("filterConfig")] FilterConfig? FilterConfig = null,
    [property: JsonPropertyName("status")] string Status = "",
    [property: JsonPropertyName("type")] string Type = "HTTP",
    [property: JsonPropertyName("typeConfig")] IReadOnlyDictionary<string, JsonElement>? TypeConfig = null,
    [property: JsonPropertyName("rateLimitRps")] int RateLimitRps = 0,
    [property: JsonPropertyName("timeoutMs")] int TimeoutMs = 0,
    [property: JsonPropertyName("retryPolicy")] RetryPolicy? RetryPolicy = null,
    [property: JsonPropertyName("headers")] IReadOnlyDictionary<string, JsonElement>? Headers = null,
    [property: JsonPropertyName("authType")] string AuthType = "",
    [property: JsonPropertyName("oauth2Config")] OAuth2Config? Oauth2Config = null,
    [property: JsonPropertyName("mtlsCert")] string MtlsCert = "",
    [property: JsonPropertyName("mtlsKey")] string MtlsKey = "",
    [property: JsonPropertyName("deliveryMode")] string DeliveryMode = "",
    [property: JsonPropertyName("pollApiKeyPrefix")] string PollApiKeyPrefix = "",
    [property: JsonPropertyName("pollApiKey")] string PollApiKey = "",
    [property: JsonPropertyName("ordered")] bool Ordered = false,
    [property: JsonPropertyName("blockedDeliveryId")] string? BlockedDeliveryId = null,
    [property: JsonPropertyName("healthScore")] double HealthScore = 1.0,
    [property: JsonPropertyName("disabledReason")] string? DisabledReason = null,
    [property: JsonPropertyName("healthConfig")] HealthConfig? HealthConfig = null,
    [property: JsonPropertyName("outputFormat")] string OutputFormat = "default",
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);
