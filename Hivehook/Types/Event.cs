using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// An ingested webhook event.
/// </summary>
public sealed record Event(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("sourceId")] string SourceId = "",
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey = "",
    [property: JsonPropertyName("eventType")] string EventType = "",
    [property: JsonPropertyName("headers")] IReadOnlyDictionary<string, JsonElement>? Headers = null,
    [property: JsonPropertyName("rawBody")] string? RawBody = null,
    [property: JsonPropertyName("status")] string Status = "",
    [property: JsonPropertyName("receivedAt")] string ReceivedAt = ""
);
