using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage delivery destinations.</summary>
public sealed class DestinationService : BaseService
{
    private const string Fragment = "id name url signingSecret status type typeConfig timeoutMs rateLimitRps retryPolicy { maxAttempts initialDelay maxDelay backoffFactor } headers authType oauth2Config { tokenUrl clientId clientSecret scopes audience } mtlsCert mtlsKey deliveryMode pollApiKeyPrefix pollApiKey ordered blockedDeliveryId healthScore disabledReason healthConfig { windowHours disableBelow } outputFormat createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public DestinationService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List destinations with optional filtering and pagination.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of destinations.</returns>
    public async Task<ListResult<Destination>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($status: DestinationStatus, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            destinations(status: $status, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "status", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<Destination>(GetField(data, "destinations"));
    }

    /// <summary>Fetch a single destination by id.</summary>
    /// <param name="id">Destination UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The destination, or <c>null</c> if not found.</returns>
    public async Task<Destination?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ destination(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<Destination>(GetField(data, "destination"));
    }

    /// <summary>Create a destination.</summary>
    /// <param name="input">Create input payload matching <c>CreateDestinationInput</c>.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created destination.</returns>
    public async Task<Destination> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateDestinationInput!) {{ createDestination(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Destination>(GetField(data, "createDestination"));
    }

    /// <summary>Update a destination.</summary>
    /// <param name="id">Destination UUID.</param>
    /// <param name="input">Update input payload matching <c>UpdateDestinationInput</c>.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated destination.</returns>
    public async Task<Destination> UpdateAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateDestinationInput!) {{ updateDestination(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Destination>(GetField(data, "updateDestination"));
    }

    /// <summary>Delete a destination.</summary>
    /// <param name="id">Destination UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteDestination(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteDestination").GetBoolean();
    }

    /// <summary>Rotate the destination's signing secret.</summary>
    /// <param name="id">Destination UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The destination with the rotated secret.</returns>
    public async Task<Destination> RotateSecretAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!) {{ rotateDestinationSecret(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Destination>(GetField(data, "rotateDestinationSecret"));
    }

    /// <summary>Poll pending deliveries for a poll-mode destination.</summary>
    /// <param name="destinationId">Destination UUID.</param>
    /// <param name="cursor">Optional pagination cursor.</param>
    /// <param name="limit">Optional batch size.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of deliveries available for processing.</returns>
    public async Task<ListResult<Delivery>> PollDeliveriesAsync(string destinationId, string? cursor = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        const string deliveryFragment = "id eventId subscriptionId destinationId status attempts maxAttempts nextAttemptAt createdAt";
        var query = $@"query($destinationId: UUID!, $cursor: String, $limit: Int) {{
            pollDeliveries(destinationId: $destinationId, cursor: $cursor, limit: $limit) {{
                nodes {{ {deliveryFragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = new Dictionary<string, object?> { ["destinationId"] = destinationId };
        if (cursor != null) vars["cursor"] = cursor;
        if (limit != null) vars["limit"] = limit;
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<Delivery>(GetField(data, "pollDeliveries"));
    }

    /// <summary>Acknowledge a batch of polled deliveries.</summary>
    /// <param name="destinationId">Destination UUID.</param>
    /// <param name="deliveryIds">Delivery UUIDs to acknowledge.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Number of deliveries acknowledged.</returns>
    public async Task<int> AckDeliveriesAsync(string destinationId, IReadOnlyList<string> deliveryIds, CancellationToken cancellationToken = default)
    {
        var query = "mutation($destinationId: UUID!, $deliveryIds: [UUID!]!) { ackDeliveries(destinationId: $destinationId, deliveryIds: $deliveryIds) }";
        var data = await Transport.ExecuteAsync(query, new() { ["destinationId"] = destinationId, ["deliveryIds"] = deliveryIds }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "ackDeliveries").GetInt32();
    }

    /// <summary>Regenerate the destination's poll-mode API key.</summary>
    /// <param name="destinationId">Destination UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The destination with the new poll key.</returns>
    public async Task<Destination> RegeneratePollApiKeyAsync(string destinationId, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($destinationId: UUID!) {{ regeneratePollApiKey(destinationId: $destinationId) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["destinationId"] = destinationId }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Destination>(GetField(data, "regeneratePollApiKey"));
    }

    /// <summary>Skip a DLQ entry blocking ordered delivery.</summary>
    /// <param name="id">DLQ entry UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> SkipDlqEntryAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { skipDLQEntry(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "skipDLQEntry").GetBoolean();
    }

    /// <summary>Fetches every page and yields each Destination as an async stream.</summary>
    public async IAsyncEnumerable<Destination> ListAllAsync(Dictionary<string, object?>? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
