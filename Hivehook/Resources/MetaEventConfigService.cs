using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage meta-event webhook configurations.</summary>
public sealed class MetaEventConfigService : BaseService
{
    private const string Fragment = "id name url signingSecret eventTypes enabled createdAt";

    public MetaEventConfigService(GraphQLTransport transport) : base(transport) { }

    public async Task<ListResult<MetaEventConfig>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            metaEventConfigs(search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<MetaEventConfig>(GetField(data, "metaEventConfigs"));
    }

    public async Task<MetaEventConfig?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ metaEventConfig(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<MetaEventConfig>(GetField(data, "metaEventConfig"));
    }

    public async Task<MetaEventConfig> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateMetaEventConfigInput!) {{ createMetaEventConfig(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<MetaEventConfig>(GetField(data, "createMetaEventConfig"));
    }

    public async Task<MetaEventConfig> UpdateAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateMetaEventConfigInput!) {{ updateMetaEventConfig(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<MetaEventConfig>(GetField(data, "updateMetaEventConfig"));
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteMetaEventConfig(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteMetaEventConfig").GetBoolean();
    }
}
