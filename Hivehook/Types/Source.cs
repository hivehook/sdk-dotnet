using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// A webhook source (provider connection).
/// </summary>
public sealed record Source(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("slug")] string Slug = "",
    [property: JsonPropertyName("providerType")] string ProviderType = "",
    [property: JsonPropertyName("verifyConfig")] IReadOnlyDictionary<string, JsonElement>? VerifyConfig = null,
    [property: JsonPropertyName("status")] string Status = "",
    [property: JsonPropertyName("rateLimitRps")] int RateLimitRps = 0,
    [property: JsonPropertyName("spikeProtection")] bool SpikeProtection = false,
    [property: JsonPropertyName("maxIngestRps")] int MaxIngestRps = 0,
    [property: JsonPropertyName("brokerConfig")] IReadOnlyDictionary<string, JsonElement>? BrokerConfig = null,
    [property: JsonPropertyName("responseConfig")] ResponseConfig? ResponseConfig = null,
    [property: JsonPropertyName("dedupConfig")] DedupConfig? DedupConfig = null,
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);
