using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Query the audit log.</summary>
public sealed class AuditLogService : BaseService
{
    private const string Fragment = "id actorType actorId actorName action resourceType resourceId orgId ipAddress userAgent details createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public AuditLogService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List audit log entries.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of audit log entries.</returns>
    public async Task<ListResult<AuditLog>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($actorType: String, $resourceType: String, $resourceId: UUID, $action: String, $since: Time, $until: Time, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            auditLogs(actorType: $actorType, resourceType: $resourceType, resourceId: $resourceId, action: $action, since: $since, until: $until, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "actorType", "resourceType", "resourceId", "action", "since", "until", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<AuditLog>(GetField(data, "auditLogs"));
    }

    /// <summary>Fetch a single audit log entry by id.</summary>
    /// <param name="id">Audit log entry UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The entry, or <c>null</c> if not found.</returns>
    public async Task<AuditLog?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ auditLog(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<AuditLog>(GetField(data, "auditLog"));
    }
}
