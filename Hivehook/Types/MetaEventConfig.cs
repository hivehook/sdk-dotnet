using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// A meta-event webhook configuration: forwards events about events
/// (delivery.failed, source.created, etc.) to an external receiver.
/// </summary>
public sealed record MetaEventConfig(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("url")] string Url = "",
    [property: JsonPropertyName("signingSecret")] string SigningSecret = "",
    [property: JsonPropertyName("eventTypes")] IReadOnlyList<string>? EventTypes = null,
    [property: JsonPropertyName("enabled")] bool Enabled = true,
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);
