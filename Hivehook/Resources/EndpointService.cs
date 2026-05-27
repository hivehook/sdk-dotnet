using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage outbound webhook endpoints.</summary>
public sealed class EndpointService : BaseService
{
    private const string Fragment = "id applicationId url signingSecret status type typeConfig rateLimitRps timeoutMs retryPolicy { maxAttempts initialDelay maxDelay backoffFactor } filterConfig { eventTypes regex bodyMatch { path value operator } rules { path operator value rules { path operator value } } } headers authType oauth2Config { tokenUrl clientId clientSecret scopes audience } mtlsCert mtlsKey deliveryMode pollApiKeyPrefix pollApiKey ordered blockedDeliveryId healthScore disabledReason healthConfig { windowHours disableBelow } outputFormat createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public EndpointService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List endpoints.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of endpoints.</returns>
    public async Task<ListResult<Endpoint>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($applicationId: UUID, $status: EndpointStatus, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            endpoints(applicationId: $applicationId, status: $status, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "applicationId", "status", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<Endpoint>(GetField(data, "endpoints"));
    }

    /// <summary>Fetch a single endpoint by id.</summary>
    /// <param name="id">Endpoint UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The endpoint, or <c>null</c> if not found.</returns>
    public async Task<Endpoint?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ endpoint(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<Endpoint>(GetField(data, "endpoint"));
    }

    /// <summary>Create an endpoint.</summary>
    /// <param name="input">Create input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created endpoint.</returns>
    public async Task<Endpoint> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateEndpointInput!) {{ createEndpoint(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Endpoint>(GetField(data, "createEndpoint"));
    }

    /// <summary>Update an endpoint.</summary>
    /// <param name="id">Endpoint UUID.</param>
    /// <param name="input">Update input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated endpoint.</returns>
    public async Task<Endpoint> UpdateAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateEndpointInput!) {{ updateEndpoint(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Endpoint>(GetField(data, "updateEndpoint"));
    }

    /// <summary>Delete an endpoint.</summary>
    /// <param name="id">Endpoint UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteEndpoint(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteEndpoint").GetBoolean();
    }

    /// <summary>Rotate the endpoint's signing secret.</summary>
    /// <param name="id">Endpoint UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The endpoint with the rotated secret.</returns>
    public async Task<Endpoint> RotateSecretAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!) {{ rotateEndpointSecret(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Endpoint>(GetField(data, "rotateEndpointSecret"));
    }

    /// <summary>Poll pending outbound deliveries for a poll-mode endpoint.</summary>
    /// <param name="endpointId">Endpoint UUID.</param>
    /// <param name="cursor">Optional pagination cursor.</param>
    /// <param name="limit">Optional batch size.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of outbound deliveries available for processing.</returns>
    public async Task<ListResult<OutboundDelivery>> PollDeliveriesAsync(string endpointId, string? cursor = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        const string deliveryFragment = "id messageId endpointId status attempts maxAttempts nextAttemptAt createdAt";
        var query = $@"query($endpointId: UUID!, $cursor: String, $limit: Int) {{
            pollOutboundDeliveries(endpointId: $endpointId, cursor: $cursor, limit: $limit) {{
                nodes {{ {deliveryFragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = new Dictionary<string, object?> { ["endpointId"] = endpointId };
        if (cursor != null) vars["cursor"] = cursor;
        if (limit != null) vars["limit"] = limit;
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<OutboundDelivery>(GetField(data, "pollOutboundDeliveries"));
    }

    /// <summary>Acknowledge a batch of polled outbound deliveries.</summary>
    /// <param name="endpointId">Endpoint UUID.</param>
    /// <param name="deliveryIds">Delivery UUIDs to acknowledge.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Number of deliveries acknowledged.</returns>
    public async Task<int> AckDeliveriesAsync(string endpointId, IReadOnlyList<string> deliveryIds, CancellationToken cancellationToken = default)
    {
        var query = "mutation($endpointId: UUID!, $deliveryIds: [UUID!]!) { ackOutboundDeliveries(endpointId: $endpointId, deliveryIds: $deliveryIds) }";
        var data = await Transport.ExecuteAsync(query, new() { ["endpointId"] = endpointId, ["deliveryIds"] = deliveryIds }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "ackOutboundDeliveries").GetInt32();
    }

    /// <summary>Regenerate the endpoint's poll-mode API key.</summary>
    /// <param name="endpointId">Endpoint UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The endpoint with the new poll key.</returns>
    public async Task<Endpoint> RegeneratePollApiKeyAsync(string endpointId, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($endpointId: UUID!) {{ regenerateOutboundPollApiKey(endpointId: $endpointId) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["endpointId"] = endpointId }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Endpoint>(GetField(data, "regenerateOutboundPollApiKey"));
    }

    /// <summary>Skip an outbound DLQ entry blocking ordered delivery.</summary>
    /// <param name="id">DLQ entry UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> SkipOutboundDlqEntryAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { skipOutboundDlqEntry(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "skipOutboundDlqEntry").GetBoolean();
    }

    /// <summary>Fetches every page and yields each Endpoint as an async stream.</summary>
    public async IAsyncEnumerable<Endpoint> ListAllAsync(Dictionary<string, object?>? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
