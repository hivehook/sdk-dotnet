using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage stream consumers.</summary>
public sealed class StreamConsumerService : BaseService
{
    private const string Fragment = "id streamId name cursorSequence createdAt updatedAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public StreamConsumerService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List consumers attached to a stream.</summary>
    /// <param name="streamId">Stream UUID.</param>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of consumers.</returns>
    public async Task<ListResult<StreamConsumer>> ListAsync(string streamId, Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($streamId: UUID!, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            streamConsumers(streamId: $streamId, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "search", "limit", "offset", "after", "first");
        vars["streamId"] = streamId;
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<StreamConsumer>(GetField(data, "streamConsumers"));
    }

    /// <summary>Fetch a consumer by id.</summary>
    /// <param name="id">Consumer UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The consumer, or <c>null</c> if not found.</returns>
    public async Task<StreamConsumer?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ streamConsumer(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<StreamConsumer>(GetField(data, "streamConsumer"));
    }

    /// <summary>Create a consumer on a stream.</summary>
    /// <param name="input">Create input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created consumer.</returns>
    public async Task<StreamConsumer> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateStreamConsumerInput!) {{ createStreamConsumer(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<StreamConsumer>(GetField(data, "createStreamConsumer"));
    }

    /// <summary>Delete a consumer.</summary>
    /// <param name="id">Consumer UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteStreamConsumer(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteStreamConsumer").GetBoolean();
    }

    /// <summary>Advance the consumer's cursor sequence.</summary>
    /// <param name="id">Consumer UUID.</param>
    /// <param name="sequence">New cursor sequence value.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated consumer.</returns>
    public async Task<StreamConsumer> AdvanceCursorAsync(string id, long sequence, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $sequence: Int!) {{ advanceConsumerCursor(id: $id, sequence: $sequence) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["sequence"] = sequence }, cancellationToken).ConfigureAwait(false);
        return Deserialize<StreamConsumer>(GetField(data, "advanceConsumerCursor"));
    }
}
