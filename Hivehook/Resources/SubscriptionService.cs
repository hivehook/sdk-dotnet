using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage subscriptions linking sources to destinations.</summary>
public sealed class SubscriptionService : BaseService
{
    private const string Fragment = "id name sourceId destinationId filterConfig { eventTypes regex bodyMatch { path value operator } rules { path operator value rules { path operator value } } } transformConfig { envelope headers } enabled createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public SubscriptionService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List subscriptions with optional filtering and pagination.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of subscriptions.</returns>
    public async Task<ListResult<Subscription>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($sourceId: UUID, $destinationId: UUID, $enabled: Boolean, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            subscriptions(sourceId: $sourceId, destinationId: $destinationId, enabled: $enabled, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "sourceId", "destinationId", "enabled", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<Subscription>(GetField(data, "subscriptions"));
    }

    /// <summary>Fetch a single subscription by id.</summary>
    /// <param name="id">Subscription UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The subscription, or <c>null</c> if not found.</returns>
    public async Task<Subscription?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ subscription(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<Subscription>(GetField(data, "subscription"));
    }

    /// <summary>Create a subscription.</summary>
    /// <param name="input">Create input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created subscription.</returns>
    public async Task<Subscription> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateSubscriptionInput!) {{ createSubscription(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Subscription>(GetField(data, "createSubscription"));
    }

    /// <summary>Update a subscription.</summary>
    /// <param name="id">Subscription UUID.</param>
    /// <param name="input">Update input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated subscription.</returns>
    public async Task<Subscription> UpdateAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateSubscriptionInput!) {{ updateSubscription(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Subscription>(GetField(data, "updateSubscription"));
    }

    /// <summary>Delete a subscription.</summary>
    /// <param name="id">Subscription UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteSubscription(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteSubscription").GetBoolean();
    }
}
