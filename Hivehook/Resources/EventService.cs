using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Read ingested webhook events.</summary>
public sealed class EventService : BaseService
{
    private const string Fragment = "id sourceId idempotencyKey eventType headers rawBody status receivedAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public EventService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List events with optional filtering and pagination.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of events.</returns>
    public async Task<ListResult<Event>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($sourceId: UUID, $eventType: String, $status: EventStatus, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            events(sourceId: $sourceId, eventType: $eventType, status: $status, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "sourceId", "eventType", "status", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<Event>(GetField(data, "events"));
    }

    /// <summary>Fetch a single event by id.</summary>
    /// <param name="id">Event UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The event, or <c>null</c> if not found.</returns>
    public async Task<Event?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ event(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<Event>(GetField(data, "event"));
    }

    /// <summary>Iterate every event matching the filter, lazily fetching pages.</summary>
    /// <param name="options">Optional filter variables. <c>limit</c> sets the page size; <c>offset</c> is managed internally.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An async stream of <see cref="Event"/> records.</returns>
    public async IAsyncEnumerable<Event> ListAllAsync(Dictionary<string, object?>? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageOptions = options == null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(options);
        if (!pageOptions.TryGetValue("limit", out var limitObj) || limitObj == null)
            pageOptions["limit"] = 100;
        var offset = 0;
        if (pageOptions.TryGetValue("offset", out var offsetObj) && offsetObj is int o)
            offset = o;

        while (true)
        {
            pageOptions["offset"] = offset;
            var page = await ListAsync(pageOptions, cancellationToken).ConfigureAwait(false);
            foreach (var node in page.Nodes)
                yield return node;
            if (!page.PageInfo.HasNextPage || page.Nodes.Count == 0)
                yield break;
            offset += page.Nodes.Count;
        }
    }
}
