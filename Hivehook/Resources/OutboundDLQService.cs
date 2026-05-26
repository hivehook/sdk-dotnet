using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Inspect and manage the outbound dead-letter queue.</summary>
public sealed class OutboundDLQService : BaseService
{
    private const string Fragment = "id deliveryId messageId lastError replayedAt createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public OutboundDLQService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List outbound DLQ entries.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of outbound DLQ entries.</returns>
    public async Task<ListResult<OutboundDLQEntry>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($messageId: UUID, $replayed: Boolean, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            outboundDlqEntries(messageId: $messageId, replayed: $replayed, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "messageId", "replayed", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<OutboundDLQEntry>(GetField(data, "outboundDlqEntries"));
    }

    /// <summary>Replay a single outbound DLQ entry.</summary>
    /// <param name="id">DLQ entry UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> ReplayAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { replayOutboundDlqEntry(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "replayOutboundDlqEntry").GetBoolean();
    }

    /// <summary>Replay every outbound DLQ entry.</summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The total number of deliveries re-enqueued.</returns>
    public async Task<ReplayResult> ReplayAllAsync(CancellationToken cancellationToken = default)
    {
        var query = "mutation { replayAllOutboundDlq { deliveries } }";
        var data = await Transport.ExecuteAsync(query, null, cancellationToken).ConfigureAwait(false);
        return Deserialize<ReplayResult>(GetField(data, "replayAllOutboundDlq"));
    }

    /// <summary>Purge outbound DLQ entries older than a duration.</summary>
    /// <param name="olderThan">Go duration string (default "168h").</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The number of entries purged.</returns>
    public async Task<PurgeResult> PurgeAsync(string olderThan = "168h", CancellationToken cancellationToken = default)
    {
        var query = "mutation($olderThan: String!) { purgeOutboundDlq(olderThan: $olderThan) { purged } }";
        var data = await Transport.ExecuteAsync(query, new() { ["olderThan"] = olderThan }, cancellationToken).ConfigureAwait(false);
        return Deserialize<PurgeResult>(GetField(data, "purgeOutboundDlq"));
    }
}
