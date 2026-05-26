using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// An audit log entry for a user or system action.
/// </summary>
public sealed record AuditLog(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("actorType")] string ActorType = "",
    [property: JsonPropertyName("actorId")] string ActorId = "",
    [property: JsonPropertyName("actorName")] string ActorName = "",
    [property: JsonPropertyName("action")] string Action = "",
    [property: JsonPropertyName("resourceType")] string ResourceType = "",
    [property: JsonPropertyName("resourceId")] string ResourceId = "",
    [property: JsonPropertyName("orgId")] string OrgId = "",
    [property: JsonPropertyName("ipAddress")] string IpAddress = "",
    [property: JsonPropertyName("userAgent")] string UserAgent = "",
    [property: JsonPropertyName("details")] JsonElement? Details = null,
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);
