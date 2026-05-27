using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage API keys.</summary>
public sealed class APIKeyService : BaseService
{
    private const string Fragment = "id name keyPrefix scopes sourceIds createdAt expiresAt revokedAt lastUsedAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public APIKeyService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List API keys.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of API keys.</returns>
    public async Task<ListResult<APIKey>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($search: String, $limit: Int, $offset: Int) {{
            apiKeys(search: $search, limit: $limit, offset: $offset) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "search", "limit", "offset");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<APIKey>(GetField(data, "apiKeys"));
    }

    /// <summary>Fetch a single API key by id.</summary>
    /// <param name="id">API key UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The API key, or <c>null</c> if not found.</returns>
    public async Task<APIKey?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ apiKey(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<APIKey>(GetField(data, "apiKey"));
    }

    /// <summary>Create an API key. The raw secret is only returned on creation.</summary>
    /// <param name="input">Create input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created API key together with its raw secret.</returns>
    public async Task<APIKeyWithSecret> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateAPIKeyInput!) {{ createAPIKey(input: $input) {{ apiKey {{ {Fragment} }} rawKey }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<APIKeyWithSecret>(GetField(data, "createAPIKey"));
    }

    /// <summary>Revoke an API key.</summary>
    /// <param name="id">API key UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> RevokeAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { revokeAPIKey(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "revokeAPIKey").GetBoolean();
    }

    /// <summary>Fetches every page and yields each APIKey as an async stream.</summary>
    public async IAsyncEnumerable<APIKey> ListAllAsync(Dictionary<string, object?>? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
