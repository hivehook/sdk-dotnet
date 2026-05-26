using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Read inbound delivery attempts.</summary>
public sealed class DeliveryService : BaseService
{
    private const string Fragment = "id eventId subscriptionId destinationId status attempts maxAttempts nextAttemptAt createdAt";
    private const string DetailFragment = Fragment + " deliveryAttempts { id deliveryId attemptNumber responseStatus responseBody error durationMs attemptedAt }";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public DeliveryService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List deliveries with optional filtering and pagination.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of deliveries.</returns>
    public async Task<ListResult<Delivery>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($eventId: UUID, $destinationId: UUID, $subscriptionId: UUID, $status: DeliveryStatus, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            deliveries(eventId: $eventId, destinationId: $destinationId, subscriptionId: $subscriptionId, status: $status, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "eventId", "destinationId", "subscriptionId", "status", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<Delivery>(GetField(data, "deliveries"));
    }

    /// <summary>Fetch a single delivery (with full attempt history) by id.</summary>
    /// <param name="id">Delivery UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The delivery including attempt history, or <c>null</c> if not found.</returns>
    public async Task<Delivery?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ delivery(id: $id) {{ {DetailFragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<Delivery>(GetField(data, "delivery"));
    }

    /// <summary>Iterate every delivery matching the filter, lazily fetching pages.</summary>
    /// <param name="options">Optional filter variables. <c>limit</c> sets the page size; <c>offset</c> is managed internally.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An async stream of <see cref="Delivery"/> records.</returns>
    public async IAsyncEnumerable<Delivery> ListAllAsync(Dictionary<string, object?>? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
