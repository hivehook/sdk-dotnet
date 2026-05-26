using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// Aggregate gateway status snapshot.
/// </summary>
public sealed record SystemStatus(
    [property: JsonPropertyName("status")] string Status = "",
    [property: JsonPropertyName("dlqSize")] int DlqSize = 0,
    [property: JsonPropertyName("outboundDlqSize")] int OutboundDlqSize = 0,
    [property: JsonPropertyName("queueDepth")] int QueueDepth = 0,
    [property: JsonPropertyName("activeWorkers")] int ActiveWorkers = 0,
    [property: JsonPropertyName("totalWorkers")] int TotalWorkers = 0,
    [property: JsonPropertyName("uptime")] long Uptime = 0,
    [property: JsonPropertyName("version")] string Version = "",
    [property: JsonPropertyName("sourcesTotal")] int SourcesTotal = 0,
    [property: JsonPropertyName("destinationsTotal")] int DestinationsTotal = 0,
    [property: JsonPropertyName("subscriptionsTotal")] int SubscriptionsTotal = 0,
    [property: JsonPropertyName("eventsTotal")] long EventsTotal = 0,
    [property: JsonPropertyName("eventsFailed")] long EventsFailed = 0,
    [property: JsonPropertyName("deliveriesTotal")] long DeliveriesTotal = 0,
    [property: JsonPropertyName("deliveriesPending")] long DeliveriesPending = 0,
    [property: JsonPropertyName("deliveriesDelivered")] long DeliveriesDelivered = 0,
    [property: JsonPropertyName("messagesTotal")] long MessagesTotal = 0,
    [property: JsonPropertyName("outboundDeliveriesTotal")] long OutboundDeliveriesTotal = 0,
    [property: JsonPropertyName("outboundDeliveriesPending")] long OutboundDeliveriesPending = 0,
    [property: JsonPropertyName("outboundDeliveriesFailed")] long OutboundDeliveriesFailed = 0
);
