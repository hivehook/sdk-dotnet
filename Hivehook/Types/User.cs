using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// A user account.
/// </summary>
public sealed record User(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("organizationId")] string OrganizationId = "",
    [property: JsonPropertyName("email")] string Email = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("role")] string Role = "",
    [property: JsonPropertyName("lastLoginAt")] string? LastLoginAt = null,
    [property: JsonPropertyName("createdAt")] string CreatedAt = "",
    [property: JsonPropertyName("updatedAt")] string UpdatedAt = ""
);
