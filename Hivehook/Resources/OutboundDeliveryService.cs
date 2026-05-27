using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Read outbound delivery attempts.</summary>
public sealed class OutboundDeliveryService : BaseService
{
    private const string Fragment = "id messageId endpointId status attempts maxAttempts nextAttemptAt createdAt";
    private const string DetailFragment = Fragment + " deliveryAttempts { id deliveryId attemptNumber responseStatus responseBody error durationMs attemptedAt }";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public OutboundDeliveryService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List outbound deliveries.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of outbound deliveries.</returns>
    public async Task<ListResult<OutboundDelivery>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($messageId: UUID, $endpointId: UUID, $status: DeliveryStatus, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            outboundDeliveries(messageId: $messageId, endpointId: $endpointId, status: $status, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "messageId", "endpointId", "status", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<OutboundDelivery>(GetField(data, "outboundDeliveries"));
    }

    /// <summary>Fetch a single outbound delivery (with full attempt history) by id.</summary>
    /// <param name="id">Delivery UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The outbound delivery, or <c>null</c> if not found.</returns>
    public async Task<OutboundDelivery?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ outboundDelivery(id: $id) {{ {DetailFragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<OutboundDelivery>(GetField(data, "outboundDelivery"));
    }

    /// <summary>Fetches every page and yields each OutboundDelivery as an async stream.</summary>
    public async IAsyncEnumerable<OutboundDelivery> ListAllAsync(Dictionary<string, object?>? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
