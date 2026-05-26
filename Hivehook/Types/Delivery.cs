using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// A single delivery attempt to a destination.
/// </summary>
public sealed record DeliveryAttempt(
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
/// A scheduled or in-flight delivery to a destination.
/// </summary>
public sealed record Delivery(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("eventId")] string EventId = "",
    [property: JsonPropertyName("subscriptionId")] string SubscriptionId = "",
    [property: JsonPropertyName("destinationId")] string DestinationId = "",
    [property: JsonPropertyName("status")] string Status = "",
    [property: JsonPropertyName("attempts")] int Attempts = 0,
    [property: JsonPropertyName("maxAttempts")] int MaxAttempts = 0,
    [property: JsonPropertyName("nextAttemptAt")] string? NextAttemptAt = null,
    [property: JsonPropertyName("createdAt")] string CreatedAt = "",
    [property: JsonPropertyName("deliveryAttempts")] IReadOnlyList<DeliveryAttempt>? DeliveryAttempts = null
);

/// <summary>
/// A dead-letter-queue entry for a failed delivery.
/// </summary>
public sealed record DLQEntry(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("deliveryId")] string DeliveryId = "",
    [property: JsonPropertyName("eventId")] string EventId = "",
    [property: JsonPropertyName("lastError")] string LastError = "",
    [property: JsonPropertyName("replayedAt")] string? ReplayedAt = null,
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);
