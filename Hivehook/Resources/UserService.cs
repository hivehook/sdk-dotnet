using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage users.</summary>
public sealed class UserService : BaseService
{
    private const string Fragment = "id organizationId email name role lastLoginAt createdAt updatedAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public UserService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List users.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of users.</returns>
    public async Task<ListResult<User>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($organizationId: UUID, $search: String, $limit: Int, $offset: Int) {{
            users(organizationId: $organizationId, search: $search, limit: $limit, offset: $offset) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "organizationId", "search", "limit", "offset");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<User>(GetField(data, "users"));
    }

    /// <summary>Get the currently authenticated user.</summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The current user, or <c>null</c> if the call is unauthenticated.</returns>
    public async Task<User?> MeAsync(CancellationToken cancellationToken = default)
    {
        var query = $"query {{ me {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, null, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<User>(GetField(data, "me"));
    }

    /// <summary>Invite a user into an organization.</summary>
    /// <param name="organizationId">Organization UUID.</param>
    /// <param name="input">Invite input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The invited user.</returns>
    public async Task<User> InviteAsync(string organizationId, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($organizationId: UUID!, $input: InviteUserInput!) {{ inviteUser(organizationId: $organizationId, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["organizationId"] = organizationId, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<User>(GetField(data, "inviteUser"));
    }

    /// <summary>Remove a user.</summary>
    /// <param name="id">User UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { removeUser(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "removeUser").GetBoolean();
    }

    /// <summary>Update a user's role.</summary>
    /// <param name="id">User UUID.</param>
    /// <param name="input">Role update input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated user.</returns>
    public async Task<User> UpdateRoleAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateUserRoleInput!) {{ updateUserRole(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<User>(GetField(data, "updateUserRole"));
    }
}
