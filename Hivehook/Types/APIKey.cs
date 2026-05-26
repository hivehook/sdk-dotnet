using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// API key metadata (the raw secret is not stored).
/// </summary>
public sealed record APIKey(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("keyPrefix")] string KeyPrefix = "",
    [property: JsonPropertyName("scopes")] IReadOnlyList<string>? Scopes = null,
    [property: JsonPropertyName("sourceIds")] IReadOnlyList<string>? SourceIds = null,
    [property: JsonPropertyName("createdAt")] string CreatedAt = "",
    [property: JsonPropertyName("expiresAt")] string? ExpiresAt = null,
    [property: JsonPropertyName("revokedAt")] string? RevokedAt = null,
    [property: JsonPropertyName("lastUsedAt")] string? LastUsedAt = null
);

/// <summary>
/// A newly created API key, including the raw secret shown only once.
/// </summary>
public sealed record APIKeyWithSecret(
    [property: JsonPropertyName("apiKey")] APIKey ApiKey,
    [property: JsonPropertyName("rawKey")] string RawKey = ""
);
