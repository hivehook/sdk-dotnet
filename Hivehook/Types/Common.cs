using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// Retry policy for failed deliveries.
/// </summary>
/// <param name="MaxAttempts">Maximum number of attempts before giving up.</param>
/// <param name="InitialDelay">Initial delay before the first retry (Go duration string).</param>
/// <param name="MaxDelay">Maximum delay cap between retries (Go duration string).</param>
/// <param name="BackoffFactor">Exponential backoff multiplier.</param>
public sealed record RetryPolicy(
    [property: JsonPropertyName("maxAttempts")] int MaxAttempts = 0,
    [property: JsonPropertyName("initialDelay")] string InitialDelay = "",
    [property: JsonPropertyName("maxDelay")] string MaxDelay = "",
    [property: JsonPropertyName("backoffFactor")] double BackoffFactor = 0
);

/// <summary>
/// Single body-match rule used in subscription filtering.
/// </summary>
/// <param name="Path">JSON path to the field to match against.</param>
/// <param name="Value">Expected value.</param>
/// <param name="Operator">Comparison operator (eq, neq, contains, etc.).</param>
public sealed record BodyMatchRule(
    [property: JsonPropertyName("path")] string Path = "",
    [property: JsonPropertyName("value")] string Value = "",
    [property: JsonPropertyName("operator")] string Operator = ""
);

/// <summary>
/// Generic filter rule (recursive).
/// </summary>
/// <param name="Operator">Logical or comparison operator.</param>
/// <param name="Path">Optional JSON path for leaf rules.</param>
/// <param name="Value">Optional value for leaf rules.</param>
/// <param name="Rules">Optional nested rules for composite rules (and/or).</param>
public sealed record FilterRule(
    [property: JsonPropertyName("operator")] string Operator = "",
    [property: JsonPropertyName("path")] string? Path = null,
    [property: JsonPropertyName("value")] JsonElement? Value = null,
    [property: JsonPropertyName("rules")] IReadOnlyList<FilterRule>? Rules = null
);

/// <summary>
/// Filter configuration for a subscription or endpoint.
/// </summary>
/// <param name="EventTypes">List of event types to accept.</param>
/// <param name="Regex">List of regex patterns to match.</param>
/// <param name="BodyMatch">Body match rules.</param>
/// <param name="Rules">Generic filter rules.</param>
public sealed record FilterConfig(
    [property: JsonPropertyName("eventTypes")] IReadOnlyList<string>? EventTypes = null,
    [property: JsonPropertyName("regex")] IReadOnlyList<string>? Regex = null,
    [property: JsonPropertyName("bodyMatch")] IReadOnlyList<BodyMatchRule>? BodyMatch = null,
    [property: JsonPropertyName("rules")] IReadOnlyList<FilterRule>? Rules = null
);

/// <summary>
/// Payload transformation configuration.
/// </summary>
/// <param name="Envelope">Whether to wrap payload in envelope metadata.</param>
/// <param name="Headers">Optional header overrides.</param>
public sealed record TransformConfig(
    [property: JsonPropertyName("envelope")] bool Envelope = false,
    [property: JsonPropertyName("headers")] IReadOnlyDictionary<string, JsonElement>? Headers = null
);

/// <summary>
/// Static response configuration for ingestion endpoints.
/// </summary>
/// <param name="StatusCode">HTTP status code to return.</param>
/// <param name="Body">Response body string.</param>
/// <param name="ContentType">Response content-type.</param>
public sealed record ResponseConfig(
    [property: JsonPropertyName("statusCode")] int StatusCode = 0,
    [property: JsonPropertyName("body")] string Body = "",
    [property: JsonPropertyName("contentType")] string ContentType = ""
);

/// <summary>
/// Deduplication configuration.
/// </summary>
/// <param name="Strategy">Dedup strategy identifier.</param>
/// <param name="Fields">Fields used for deduplication.</param>
/// <param name="Window">Optional dedup window (Go duration string).</param>
public sealed record DedupConfig(
    [property: JsonPropertyName("strategy")] string Strategy = "",
    [property: JsonPropertyName("fields")] IReadOnlyList<string>? Fields = null,
    [property: JsonPropertyName("window")] string? Window = null
);

/// <summary>
/// OAuth2 client-credentials configuration for outbound auth.
/// </summary>
/// <param name="TokenUrl">OAuth2 token endpoint URL.</param>
/// <param name="ClientId">OAuth2 client id.</param>
/// <param name="ClientSecret">OAuth2 client secret.</param>
/// <param name="Scopes">Requested scopes.</param>
/// <param name="Audience">Optional audience claim.</param>
public sealed record OAuth2Config(
    [property: JsonPropertyName("tokenUrl")] string TokenUrl = "",
    [property: JsonPropertyName("clientId")] string ClientId = "",
    [property: JsonPropertyName("clientSecret")] string ClientSecret = "",
    [property: JsonPropertyName("scopes")] IReadOnlyList<string>? Scopes = null,
    [property: JsonPropertyName("audience")] string Audience = ""
);

/// <summary>
/// Endpoint/destination health configuration.
/// </summary>
/// <param name="WindowHours">Health calculation window in hours.</param>
/// <param name="DisableBelow">Score threshold below which the destination is auto-disabled.</param>
public sealed record HealthConfig(
    [property: JsonPropertyName("windowHours")] int WindowHours = 0,
    [property: JsonPropertyName("disableBelow")] double DisableBelow = 0
);

/// <summary>
/// Connection page metadata.
/// </summary>
/// <param name="Total">Total number of items.</param>
/// <param name="Limit">Page size used by the server.</param>
/// <param name="Offset">Page offset used by the server.</param>
/// <param name="EndCursor">Optional cursor for the next page.</param>
/// <param name="HasNextPage">Whether more items exist beyond this page.</param>
public sealed record PageInfo(
    [property: JsonPropertyName("total")] int Total = 0,
    [property: JsonPropertyName("limit")] int Limit = 0,
    [property: JsonPropertyName("offset")] int Offset = 0,
    [property: JsonPropertyName("endCursor")] string? EndCursor = null,
    [property: JsonPropertyName("hasNextPage")] bool HasNextPage = false
);

/// <summary>
/// Paginated list of items.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
/// <param name="Nodes">Page items.</param>
/// <param name="PageInfo">Pagination metadata.</param>
public sealed record ListResult<T>(
    [property: JsonPropertyName("nodes")] IReadOnlyList<T> Nodes,
    [property: JsonPropertyName("pageInfo")] PageInfo PageInfo
);

/// <summary>
/// Result of a DLQ replay operation.
/// </summary>
/// <param name="Deliveries">Number of deliveries re-enqueued.</param>
public sealed record ReplayResult(
    [property: JsonPropertyName("deliveries")] int Deliveries = 0
);

/// <summary>
/// Result of a DLQ purge operation.
/// </summary>
/// <param name="Purged">Number of DLQ entries removed.</param>
public sealed record PurgeResult(
    [property: JsonPropertyName("purged")] int Purged = 0
);

/// <summary>
/// OpenTelemetry OTLP exporter configuration.
/// </summary>
/// <param name="Endpoint">OTLP endpoint URL.</param>
/// <param name="Headers">Headers to send with OTLP exports.</param>
/// <param name="Insecure">Whether TLS verification is disabled.</param>
/// <param name="SampleRate">Trace sample rate (0..1).</param>
public sealed record OTLPConfig(
    [property: JsonPropertyName("endpoint")] string Endpoint = "",
    [property: JsonPropertyName("headers")] IReadOnlyDictionary<string, string>? Headers = null,
    [property: JsonPropertyName("insecure")] bool Insecure = false,
    [property: JsonPropertyName("sampleRate")] double SampleRate = 0
);

/// <summary>
/// Alert rule email-channel configuration.
/// </summary>
/// <param name="To">Recipient email addresses.</param>
/// <param name="SubjectTemplate">Optional subject template.</param>
public sealed record EmailAlertConfig(
    [property: JsonPropertyName("to")] IReadOnlyList<string>? To = null,
    [property: JsonPropertyName("subjectTemplate")] string? SubjectTemplate = null
);

/// <summary>
/// Alert rule Slack-channel configuration.
/// </summary>
/// <param name="WebhookUrl">Slack incoming webhook URL.</param>
/// <param name="Channel">Optional Slack channel override.</param>
public sealed record SlackAlertConfig(
    [property: JsonPropertyName("webhookUrl")] string WebhookUrl = "",
    [property: JsonPropertyName("channel")] string? Channel = null
);

/// <summary>
/// A short-lived token granting access to the embeddable customer portal.
/// </summary>
/// <param name="Token">Bearer token value.</param>
/// <param name="ExpiresAt">Token expiry timestamp.</param>
public sealed record PortalToken(
    [property: JsonPropertyName("token")] string Token = "",
    [property: JsonPropertyName("expiresAt")] string ExpiresAt = ""
);
