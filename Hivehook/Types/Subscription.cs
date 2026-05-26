using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// A subscription connecting a source to a destination.
/// </summary>
public sealed record Subscription(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("sourceId")] string SourceId = "",
    [property: JsonPropertyName("destinationId")] string DestinationId = "",
    [property: JsonPropertyName("filterConfig")] FilterConfig? FilterConfig = null,
    [property: JsonPropertyName("transformConfig")] TransformConfig? TransformConfig = null,
    [property: JsonPropertyName("enabled")] bool Enabled = true,
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);
