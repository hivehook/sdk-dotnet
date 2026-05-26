using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// An organization (tenant) in Hivehook.
/// </summary>
public sealed record Organization(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("slug")] string Slug = "",
    [property: JsonPropertyName("ssoEnabled")] bool SsoEnabled = false,
    [property: JsonPropertyName("ssoProvider")] string? SsoProvider = null,
    [property: JsonPropertyName("retentionEvents")] int RetentionEvents = 0,
    [property: JsonPropertyName("retentionMessages")] int RetentionMessages = 0,
    [property: JsonPropertyName("otlpConfig")] OTLPConfig? OtlpConfig = null,
    [property: JsonPropertyName("createdAt")] string CreatedAt = "",
    [property: JsonPropertyName("updatedAt")] string UpdatedAt = ""
);
