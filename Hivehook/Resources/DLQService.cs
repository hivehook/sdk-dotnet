using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Inspect and manage the inbound dead-letter queue.</summary>
public sealed class DLQService : BaseService
{
    private const string Fragment = "id deliveryId eventId lastError replayedAt createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public DLQService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List DLQ entries.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of DLQ entries.</returns>
    public async Task<ListResult<DLQEntry>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($eventId: UUID, $replayed: Boolean, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            dlqEntries(eventId: $eventId, replayed: $replayed, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "eventId", "replayed", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<DLQEntry>(GetField(data, "dlqEntries"));
    }

    /// <summary>Replay a single DLQ entry.</summary>
    /// <param name="id">DLQ entry UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> ReplayAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { replayDLQEntry(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "replayDLQEntry").GetBoolean();
    }

    /// <summary>Replay every entry currently in the DLQ.</summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The total number of deliveries re-enqueued.</returns>
    public async Task<ReplayResult> ReplayAllAsync(CancellationToken cancellationToken = default)
    {
        var query = "mutation { replayAllDLQ { deliveries } }";
        var data = await Transport.ExecuteAsync(query, null, cancellationToken).ConfigureAwait(false);
        return Deserialize<ReplayResult>(GetField(data, "replayAllDLQ"));
    }

    /// <summary>Purge DLQ entries older than a duration.</summary>
    /// <param name="olderThan">Go duration string (default "168h").</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The number of entries purged.</returns>
    public async Task<PurgeResult> PurgeAsync(string olderThan = "168h", CancellationToken cancellationToken = default)
    {
        var query = "mutation($olderThan: String!) { purgeDLQ(olderThan: $olderThan) { purged } }";
        var data = await Transport.ExecuteAsync(query, new() { ["olderThan"] = olderThan }, cancellationToken).ConfigureAwait(false);
        return Deserialize<PurgeResult>(GetField(data, "purgeDLQ"));
    }

    /// <summary>Fetches every page and yields each DLQEntry as an async stream.</summary>
    public async IAsyncEnumerable<DLQEntry> ListAllAsync(Dictionary<string, object?>? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
