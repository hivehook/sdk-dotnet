using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// An event stream.
/// </summary>
public sealed record Stream(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("applicationId")] string ApplicationId = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("status")] string Status = "",
    [property: JsonPropertyName("retentionDays")] int RetentionDays = 0,
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);

/// <summary>
/// A consumer pointer on a stream.
/// </summary>
public sealed record StreamConsumer(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("streamId")] string StreamId = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("cursorSequence")] long CursorSequence = 0,
    [property: JsonPropertyName("createdAt")] string CreatedAt = "",
    [property: JsonPropertyName("updatedAt")] string UpdatedAt = ""
);

/// <summary>
/// A sink that drains a stream to an external destination (S3, Kafka, etc.).
/// </summary>
public sealed record StreamSink(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("streamId")] string StreamId = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("sinkType")] string SinkType = "",
    [property: JsonPropertyName("config")] IReadOnlyDictionary<string, JsonElement>? Config = null,
    [property: JsonPropertyName("batchSize")] int BatchSize = 0,
    [property: JsonPropertyName("flushInterval")] string FlushInterval = "",
    [property: JsonPropertyName("cursorSequence")] long CursorSequence = 0,
    [property: JsonPropertyName("status")] string Status = "",
    [property: JsonPropertyName("lastFlushedAt")] string? LastFlushedAt = null,
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);
