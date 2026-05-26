using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage stream sinks (external drains for streams).</summary>
public sealed class StreamSinkService : BaseService
{
    private const string Fragment = "id streamId name sinkType config batchSize flushInterval cursorSequence status lastFlushedAt createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public StreamSinkService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List sinks for a stream.</summary>
    /// <param name="streamId">Stream UUID.</param>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of sinks.</returns>
    public async Task<ListResult<StreamSink>> ListAsync(string streamId, Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($streamId: UUID!, $status: SinkStatus, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            streamSinks(streamId: $streamId, status: $status, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "status", "search", "limit", "offset", "after", "first");
        vars["streamId"] = streamId;
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<StreamSink>(GetField(data, "streamSinks"));
    }

    /// <summary>Fetch a sink by id.</summary>
    /// <param name="id">Sink UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The sink, or <c>null</c> if not found.</returns>
    public async Task<StreamSink?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ streamSink(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<StreamSink>(GetField(data, "streamSink"));
    }

    /// <summary>Create a sink on a stream.</summary>
    /// <param name="input">Create input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created sink.</returns>
    public async Task<StreamSink> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateStreamSinkInput!) {{ createStreamSink(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<StreamSink>(GetField(data, "createStreamSink"));
    }

    /// <summary>Update a sink.</summary>
    /// <param name="id">Sink UUID.</param>
    /// <param name="input">Update input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated sink.</returns>
    public async Task<StreamSink> UpdateAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateStreamSinkInput!) {{ updateStreamSink(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<StreamSink>(GetField(data, "updateStreamSink"));
    }

    /// <summary>Delete a sink.</summary>
    /// <param name="id">Sink UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteStreamSink(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteStreamSink").GetBoolean();
    }
}
