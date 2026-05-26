using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// A JSON schema definition for an event type.
/// </summary>
public sealed record EventTypeSchema(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("eventType")] string EventType = "",
    [property: JsonPropertyName("description")] string Description = "",
    [property: JsonPropertyName("schema")] IReadOnlyDictionary<string, JsonElement>? Schema = null,
    [property: JsonPropertyName("example")] IReadOnlyDictionary<string, JsonElement>? Example = null,
    [property: JsonPropertyName("createdAt")] string CreatedAt = "",
    [property: JsonPropertyName("updatedAt")] string UpdatedAt = ""
);
