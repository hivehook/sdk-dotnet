using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// An outbound webhook message sent by an Application.
/// </summary>
public sealed record Message(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("applicationId")] string ApplicationId = "",
    [property: JsonPropertyName("eventType")] string EventType = "",
    [property: JsonPropertyName("payload")] string? Payload = null,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey = "",
    [property: JsonPropertyName("status")] string Status = "",
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);

/// <summary>
/// A single outbound delivery attempt to an Endpoint.
/// </summary>
public sealed record OutboundDeliveryAttempt(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("deliveryId")] string DeliveryId = "",
    [property: JsonPropertyName("attemptNumber")] int AttemptNumber = 0,
    [property: JsonPropertyName("responseStatus")] int ResponseStatus = 0,
    [property: JsonPropertyName("responseBody")] string ResponseBody = "",
    [property: JsonPropertyName("error")] string Error = "",
    [property: JsonPropertyName("durationMs")] int DurationMs = 0,
    [property: JsonPropertyName("attemptedAt")] string AttemptedAt = ""
);

/// <summary>
/// An outbound delivery of a Message to an Endpoint.
/// </summary>
public sealed record OutboundDelivery(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("messageId")] string MessageId = "",
    [property: JsonPropertyName("endpointId")] string EndpointId = "",
    [property: JsonPropertyName("status")] string Status = "",
    [property: JsonPropertyName("attempts")] int Attempts = 0,
    [property: JsonPropertyName("maxAttempts")] int MaxAttempts = 0,
    [property: JsonPropertyName("nextAttemptAt")] string? NextAttemptAt = null,
    [property: JsonPropertyName("createdAt")] string CreatedAt = "",
    [property: JsonPropertyName("deliveryAttempts")] IReadOnlyList<OutboundDeliveryAttempt>? DeliveryAttempts = null
);

/// <summary>
/// A dead-letter-queue entry for a failed outbound delivery.
/// </summary>
public sealed record OutboundDLQEntry(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("deliveryId")] string DeliveryId = "",
    [property: JsonPropertyName("messageId")] string MessageId = "",
    [property: JsonPropertyName("lastError")] string LastError = "",
    [property: JsonPropertyName("replayedAt")] string? ReplayedAt = null,
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);
