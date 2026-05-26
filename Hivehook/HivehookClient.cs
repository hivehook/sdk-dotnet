using System;
using System.Net.Http;
using Hivehook.Extensions;
using Hivehook.Resources;

namespace Hivehook;

/// <summary>
/// Top-level Hivehook API client. Exposes a typed service per resource group.
/// </summary>
public sealed class HivehookClient : IDisposable
{
    /// <summary>Manage webhook sources.</summary>
    public SourceService Sources { get; }
    /// <summary>Manage delivery destinations.</summary>
    public DestinationService Destinations { get; }
    /// <summary>Manage subscriptions.</summary>
    public SubscriptionService Subscriptions { get; }
    /// <summary>Read ingested events.</summary>
    public EventService Events { get; }
    /// <summary>Read inbound deliveries.</summary>
    public DeliveryService Deliveries { get; }
    /// <summary>Inbound dead-letter queue.</summary>
    public DLQService DLQ { get; }
    /// <summary>Manage API keys.</summary>
    public APIKeyService APIKeys { get; }
    /// <summary>Manage alert rules.</summary>
    public AlertRuleService AlertRules { get; }
    /// <summary>Manage event bookmarks.</summary>
    public BookmarkService Bookmarks { get; }
    /// <summary>Manage event-type JSON schemas.</summary>
    public EventTypeSchemaService EventTypeSchemas { get; }
    /// <summary>Manage outbound applications.</summary>
    public ApplicationService Applications { get; }
    /// <summary>Manage outbound endpoints.</summary>
    public EndpointService Endpoints { get; }
    /// <summary>Send outbound messages.</summary>
    public MessageService Messages { get; }
    /// <summary>Read outbound deliveries.</summary>
    public OutboundDeliveryService OutboundDeliveries { get; }
    /// <summary>Outbound dead-letter queue.</summary>
    public OutboundDLQService OutboundDLQ { get; }
    /// <summary>Gateway status.</summary>
    public StatusService Status { get; }
    /// <summary>Manage payload transformations.</summary>
    public TransformationService Transformations { get; }
    /// <summary>Customer portal tokens.</summary>
    public PortalService Portal { get; }
    /// <summary>Manage event streams.</summary>
    public StreamService Streams { get; }
    /// <summary>Manage stream consumers.</summary>
    public StreamConsumerService StreamConsumers { get; }
    /// <summary>Manage stream sinks.</summary>
    public StreamSinkService StreamSinks { get; }
    /// <summary>Manage organizations.</summary>
    public OrganizationService Organizations { get; }
    /// <summary>Manage users.</summary>
    public UserService Users { get; }
    /// <summary>Query the audit log.</summary>
    public AuditLogService AuditLogs { get; }

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    /// <summary>
    /// Create a new Hivehook client.
    /// </summary>
    /// <param name="baseUrl">Server base URL (default <c>http://localhost:8080</c>).</param>
    /// <param name="apiKey">Optional API key sent as a Bearer token.</param>
    /// <param name="httpClient">Optional caller-supplied <see cref="HttpClient"/>. When supplied,
    /// the caller retains ownership and the client will not dispose it.</param>
    /// <param name="maxRetries">Maximum number of retries for transient failures (default 2).</param>
    /// <param name="timeout">Per-request timeout (default 30s).</param>
    public HivehookClient(
        string baseUrl = "http://localhost:8080",
        string? apiKey = null,
        HttpClient? httpClient = null,
        int maxRetries = 2,
        TimeSpan? timeout = null)
    {
        _ownsHttpClient = httpClient == null;
        _httpClient = httpClient ?? new HttpClient();
        var transport = new GraphQLTransport(baseUrl, apiKey, _httpClient, maxRetries, timeout);
        Sources = new SourceService(transport);
        Destinations = new DestinationService(transport);
        Subscriptions = new SubscriptionService(transport);
        Events = new EventService(transport);
        Deliveries = new DeliveryService(transport);
        DLQ = new DLQService(transport);
        APIKeys = new APIKeyService(transport);
        AlertRules = new AlertRuleService(transport);
        Bookmarks = new BookmarkService(transport);
        EventTypeSchemas = new EventTypeSchemaService(transport);
        Applications = new ApplicationService(transport);
        Endpoints = new EndpointService(transport);
        Messages = new MessageService(transport);
        OutboundDeliveries = new OutboundDeliveryService(transport);
        OutboundDLQ = new OutboundDLQService(transport);
        Status = new StatusService(transport);
        Transformations = new TransformationService(transport);
        Portal = new PortalService(transport);
        Streams = new StreamService(transport);
        StreamConsumers = new StreamConsumerService(transport);
        StreamSinks = new StreamSinkService(transport);
        Organizations = new OrganizationService(transport);
        Users = new UserService(transport);
        AuditLogs = new AuditLogService(transport);
    }

    /// <summary>
    /// DI-friendly constructor used by <c>AddHivehook</c>. The HTTP client is owned by
    /// <see cref="IHttpClientFactory"/>; this client does not dispose it.
    /// </summary>
    public HivehookClient(HttpClient httpClient, HivehookClientOptions options)
        : this(
            (options ?? throw new ArgumentNullException(nameof(options))).BaseUrl,
            options.ApiKey,
            httpClient ?? throw new ArgumentNullException(nameof(httpClient)),
            options.MaxRetries,
            options.Timeout)
    {
    }

    /// <summary>
    /// Dispose the client. Disposes the owned <see cref="HttpClient"/> only when the
    /// client created it; a caller-supplied <see cref="HttpClient"/> is never disposed here.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
