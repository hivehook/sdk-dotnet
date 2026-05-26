using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// An outbound webhook application (tenant grouping for endpoints).
/// </summary>
public sealed record Application(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("uid")] string Uid = "",
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);
