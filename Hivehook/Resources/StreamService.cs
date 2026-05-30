using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Hivehook.Types;
// Alias to avoid collision with System.IO.Stream pulled in by ImplicitUsings.
using HivehookStream = Hivehook.Types.Stream;

namespace Hivehook.Resources;

/// <summary>Manage event streams.</summary>
public sealed class StreamService : BaseService
{
    private const string Fragment = "id applicationId name status retentionDays createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public StreamService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List streams.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of streams.</returns>
    public async Task<ListResult<HivehookStream>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($applicationId: UUID, $status: StreamStatus, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            streams(applicationId: $applicationId, status: $status, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "applicationId", "status", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<HivehookStream>(GetField(data, "streams"));
    }

    /// <summary>Fetch a stream by id.</summary>
    /// <param name="id">Stream UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The stream, or <c>null</c> if not found.</returns>
    public async Task<HivehookStream?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ stream(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<HivehookStream>(GetField(data, "stream"));
    }

    /// <summary>Create a stream.</summary>
    /// <param name="input">Create input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created stream.</returns>
    public async Task<HivehookStream> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateStreamInput!) {{ createStream(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<HivehookStream>(GetField(data, "createStream"));
    }

    /// <summary>Update a stream.</summary>
    /// <param name="id">Stream UUID.</param>
    /// <param name="input">Update input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated stream.</returns>
    public async Task<HivehookStream> UpdateAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateStreamInput!) {{ updateStream(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<HivehookStream>(GetField(data, "updateStream"));
    }

    /// <summary>Delete a stream.</summary>
    /// <param name="id">Stream UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteStream(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteStream").GetBoolean();
    }

    /// <summary>List persisted entries from a stream, ordered by sequence.</summary>
    public async Task<ListResult<StreamEntry>> EntriesAsync(string streamId, long? afterSequence = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        const string entryFragment = "id streamId sequence messageId eventType payload createdAt";
        var query = $@"query($streamId: UUID!, $afterSequence: Int, $limit: Int) {{
            streamEntries(streamId: $streamId, afterSequence: $afterSequence, limit: $limit) {{
                nodes {{ {entryFragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = new Dictionary<string, object?> { ["streamId"] = streamId };
        if (afterSequence is { } a) vars["afterSequence"] = a;
        if (limit is { } l) vars["limit"] = l;
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<StreamEntry>(GetField(data, "streamEntries"));
    }

    /// <summary>Fetches every page and yields each HivehookStream as an async stream.</summary>
    public async IAsyncEnumerable<HivehookStream> ListAllAsync(Dictionary<string, object?>? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
