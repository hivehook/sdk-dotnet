using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// A payload transformation script applied to events before delivery.
/// </summary>
public sealed record Transformation(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("description")] string Description = "",
    [property: JsonPropertyName("code")] string Code = "",
    [property: JsonPropertyName("enabled")] bool Enabled = true,
    [property: JsonPropertyName("failOpen")] bool FailOpen = false,
    [property: JsonPropertyName("timeoutMs")] int TimeoutMs = 0,
    [property: JsonPropertyName("createdAt")] string CreatedAt = "",
    [property: JsonPropertyName("updatedAt")] string UpdatedAt = ""
);

/// <summary>
/// Result of a transformation dry-run.
/// </summary>
public sealed record TransformTestResult(
    [property: JsonPropertyName("success")] bool Success = false,
    [property: JsonPropertyName("output")] IReadOnlyDictionary<string, JsonElement>? Output = null,
    [property: JsonPropertyName("error")] string Error = "",
    [property: JsonPropertyName("durationMs")] int DurationMs = 0
);
