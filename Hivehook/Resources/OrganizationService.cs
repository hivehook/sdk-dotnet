using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage organizations (tenants).</summary>
public sealed class OrganizationService : BaseService
{
    private const string Fragment = "id name slug ssoEnabled ssoProvider retentionEvents retentionMessages otlpConfig { endpoint headers insecure sampleRate } createdAt updatedAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public OrganizationService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List organizations.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of organizations.</returns>
    public async Task<ListResult<Organization>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($search: String, $limit: Int, $offset: Int) {{
            organizations(search: $search, limit: $limit, offset: $offset) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "search", "limit", "offset");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<Organization>(GetField(data, "organizations"));
    }

    /// <summary>Fetch an organization by id.</summary>
    /// <param name="id">Organization UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The organization, or <c>null</c> if not found.</returns>
    public async Task<Organization?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ organization(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<Organization>(GetField(data, "organization"));
    }

    /// <summary>Create an organization.</summary>
    /// <param name="input">Create input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created organization.</returns>
    public async Task<Organization> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateOrganizationInput!) {{ createOrganization(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Organization>(GetField(data, "createOrganization"));
    }

    /// <summary>Update an organization.</summary>
    /// <param name="id">Organization UUID.</param>
    /// <param name="input">Update input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated organization.</returns>
    public async Task<Organization> UpdateAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateOrganizationInput!) {{ updateOrganization(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Organization>(GetField(data, "updateOrganization"));
    }

    /// <summary>Delete an organization.</summary>
    /// <param name="id">Organization UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteOrganization(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteOrganization").GetBoolean();
    }

    /// <summary>Configure SSO on an organization.</summary>
    /// <param name="organizationId">Organization UUID.</param>
    /// <param name="input">SSO configuration input.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated organization.</returns>
    public async Task<Organization> ConfigureSsoAsync(string organizationId, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($organizationId: UUID!, $input: SSOConfigInput!) {{ configureSSO(organizationId: $organizationId, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["organizationId"] = organizationId, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Organization>(GetField(data, "configureSSO"));
    }

    /// <summary>Disable SSO on an organization.</summary>
    /// <param name="organizationId">Organization UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated organization.</returns>
    public async Task<Organization> DisableSsoAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($organizationId: UUID!) {{ disableSSO(organizationId: $organizationId) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["organizationId"] = organizationId }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Organization>(GetField(data, "disableSSO"));
    }

    /// <summary>Update an organization's retention windows.</summary>
    /// <param name="organizationId">Organization UUID.</param>
    /// <param name="input">Retention input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated organization.</returns>
    public async Task<Organization> UpdateRetentionAsync(string organizationId, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($organizationId: UUID!, $input: RetentionInput!) {{ updateOrganizationRetention(organizationId: $organizationId, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["organizationId"] = organizationId, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Organization>(GetField(data, "updateOrganizationRetention"));
    }

    /// <summary>Permanently delete all data for an organization.</summary>
    /// <param name="organizationId">Organization UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteDataAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        var query = "mutation($organizationId: UUID!) { deleteOrganizationData(organizationId: $organizationId) }";
        var data = await Transport.ExecuteAsync(query, new() { ["organizationId"] = organizationId }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteOrganizationData").GetBoolean();
    }

    /// <summary>Export an organization's data. The shape of the export is server-defined.</summary>
    /// <param name="organizationId">Organization UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Raw JSON export payload.</returns>
    public async Task<JsonElement> ExportDataAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        var query = "mutation($organizationId: UUID!) { exportOrganizationData(organizationId: $organizationId) }";
        var data = await Transport.ExecuteAsync(query, new() { ["organizationId"] = organizationId }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "exportOrganizationData");
    }

    /// <summary>Configure OTLP telemetry export for an organization.</summary>
    /// <param name="organizationId">Organization UUID.</param>
    /// <param name="input">OTLP configuration input.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated organization.</returns>
    public async Task<Organization> ConfigureOtlpAsync(string organizationId, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($organizationId: UUID!, $input: OTLPConfigInput!) {{ configureOTLP(organizationId: $organizationId, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["organizationId"] = organizationId, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Organization>(GetField(data, "configureOTLP"));
    }

    /// <summary>Disable OTLP telemetry export.</summary>
    /// <param name="organizationId">Organization UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated organization.</returns>
    public async Task<Organization> DisableOtlpAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($organizationId: UUID!) {{ disableOTLP(organizationId: $organizationId) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["organizationId"] = organizationId }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Organization>(GetField(data, "disableOTLP"));
    }

    /// <summary>Fetches every page and yields each Organization as an async stream.</summary>
    public async IAsyncEnumerable<Organization> ListAllAsync(Dictionary<string, object?>? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
