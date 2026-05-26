using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// A user-saved bookmark on an event.
/// </summary>
public sealed record Bookmark(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("eventId")] string EventId = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("notes")] string Notes = "",
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);
