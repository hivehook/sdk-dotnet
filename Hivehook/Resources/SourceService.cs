using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage webhook sources (provider connections).</summary>
public sealed class SourceService : BaseService
{
    private const string Fragment = "id name slug providerType verifyConfig status rateLimitRps spikeProtection maxIngestRps brokerConfig responseConfig { statusCode body contentType } dedupConfig { strategy fields window } createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public SourceService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List sources with optional filtering and pagination.</summary>
    /// <param name="options">Optional filter/pagination variables (status, providerType, search, limit, offset, after, first).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of sources.</returns>
    public async Task<ListResult<Source>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($status: SourceStatus, $providerType: String, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            sources(status: $status, providerType: $providerType, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "status", "providerType", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<Source>(GetField(data, "sources"));
    }

    /// <summary>Fetch a single source by id.</summary>
    /// <param name="id">Source UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The source, or <c>null</c> if not found.</returns>
    public async Task<Source?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ source(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<Source>(GetField(data, "source"));
    }

    /// <summary>Create a source.</summary>
    /// <param name="input">Create input payload matching <c>CreateSourceInput</c>.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created source.</returns>
    public async Task<Source> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateSourceInput!) {{ createSource(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Source>(GetField(data, "createSource"));
    }

    /// <summary>Update a source.</summary>
    /// <param name="id">Source UUID.</param>
    /// <param name="input">Update input payload matching <c>UpdateSourceInput</c>.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated source.</returns>
    public async Task<Source> UpdateAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateSourceInput!) {{ updateSource(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Source>(GetField(data, "updateSource"));
    }

    /// <summary>Delete a source.</summary>
    /// <param name="id">Source UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteSource(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteSource").GetBoolean();
    }

    /// <summary>Rotate the source's signing secret.</summary>
    /// <param name="id">Source UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The source with the rotated secret.</returns>
    public async Task<Source> RotateSecretAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!) {{ rotateSourceSecret(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Source>(GetField(data, "rotateSourceSecret"));
    }

    /// <summary>Clear any secondary (legacy) signing secret on the source.</summary>
    /// <param name="id">Source UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated source.</returns>
    public async Task<Source> ClearSecondarySecretAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!) {{ clearSourceSecondarySecret(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Source>(GetField(data, "clearSourceSecondarySecret"));
    }

    /// <summary>Iterate every source matching the filter, lazily fetching pages.</summary>
    /// <param name="options">Optional filter variables. <c>limit</c> sets the page size; <c>offset</c> is managed internally.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An async stream of <see cref="Source"/> records.</returns>
    public async IAsyncEnumerable<Source> ListAllAsync(Dictionary<string, object?>? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
