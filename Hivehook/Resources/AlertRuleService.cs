using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage alert rules.</summary>
public sealed class AlertRuleService : BaseService
{
    private const string Fragment = "id name conditionType threshold webhookUrl channel emailConfig { to subjectTemplate } slackConfig { webhookUrl channel } cooldown enabled createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public AlertRuleService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List alert rules.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of alert rules.</returns>
    public async Task<ListResult<AlertRule>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($enabled: Boolean, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            alertRules(enabled: $enabled, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "enabled", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<AlertRule>(GetField(data, "alertRules"));
    }

    /// <summary>Fetch a single alert rule by id.</summary>
    /// <param name="id">Alert rule UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The alert rule, or <c>null</c> if not found.</returns>
    public async Task<AlertRule?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ alertRule(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<AlertRule>(GetField(data, "alertRule"));
    }

    /// <summary>Create an alert rule.</summary>
    /// <param name="input">Create input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created alert rule.</returns>
    public async Task<AlertRule> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateAlertRuleInput!) {{ createAlertRule(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<AlertRule>(GetField(data, "createAlertRule"));
    }

    /// <summary>Update an alert rule.</summary>
    /// <param name="id">Alert rule UUID.</param>
    /// <param name="input">Update input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated alert rule.</returns>
    public async Task<AlertRule> UpdateAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateAlertRuleInput!) {{ updateAlertRule(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<AlertRule>(GetField(data, "updateAlertRule"));
    }

    /// <summary>Delete an alert rule.</summary>
    /// <param name="id">Alert rule UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteAlertRule(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteAlertRule").GetBoolean();
    }

    /// <summary>Fire a test alert through the rule's configured channel.</summary>
    /// <param name="id">Alert rule UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> TestAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { testAlertRule(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "testAlertRule").GetBoolean();
    }
}
