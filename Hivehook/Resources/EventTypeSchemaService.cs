using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage event type JSON schemas.</summary>
public sealed class EventTypeSchemaService : BaseService
{
    private const string Fragment = "id eventType description schema example createdAt updatedAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public EventTypeSchemaService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List event-type schemas.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of schemas.</returns>
    public async Task<ListResult<EventTypeSchema>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            eventTypeSchemas(search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<EventTypeSchema>(GetField(data, "eventTypeSchemas"));
    }

    /// <summary>Fetch a single schema by id.</summary>
    /// <param name="id">Schema UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The schema, or <c>null</c> if not found.</returns>
    public async Task<EventTypeSchema?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ eventTypeSchema(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<EventTypeSchema>(GetField(data, "eventTypeSchema"));
    }

    /// <summary>Create an event-type schema.</summary>
    /// <param name="input">Create input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created schema.</returns>
    public async Task<EventTypeSchema> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateEventTypeSchemaInput!) {{ createEventTypeSchema(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<EventTypeSchema>(GetField(data, "createEventTypeSchema"));
    }

    /// <summary>Update an event-type schema.</summary>
    /// <param name="id">Schema UUID.</param>
    /// <param name="input">Update input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated schema.</returns>
    public async Task<EventTypeSchema> UpdateAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateEventTypeSchemaInput!) {{ updateEventTypeSchema(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<EventTypeSchema>(GetField(data, "updateEventTypeSchema"));
    }

    /// <summary>Delete an event-type schema.</summary>
    /// <param name="id">Schema UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteEventTypeSchema(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteEventTypeSchema").GetBoolean();
    }

    /// <summary>Fetches every page and yields each EventTypeSchema as an async stream.</summary>
    public async IAsyncEnumerable<EventTypeSchema> ListAllAsync(Dictionary<string, object?>? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
