using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage payload transformation scripts.</summary>
public sealed class TransformationService : BaseService
{
    private const string Fragment = "id name description code enabled failOpen timeoutMs createdAt updatedAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public TransformationService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List transformations.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of transformations.</returns>
    public async Task<ListResult<Transformation>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($enabled: Boolean, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            transformations(enabled: $enabled, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "enabled", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<Transformation>(GetField(data, "transformations"));
    }

    /// <summary>Fetch a transformation by id.</summary>
    /// <param name="id">Transformation UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The transformation, or <c>null</c> if not found.</returns>
    public async Task<Transformation?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ transformation(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<Transformation>(GetField(data, "transformation"));
    }

    /// <summary>Create a transformation.</summary>
    /// <param name="input">Create input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created transformation.</returns>
    public async Task<Transformation> CreateAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: CreateTransformationInput!) {{ createTransformation(input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Transformation>(GetField(data, "createTransformation"));
    }

    /// <summary>Update a transformation.</summary>
    /// <param name="id">Transformation UUID.</param>
    /// <param name="input">Update input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated transformation.</returns>
    public async Task<Transformation> UpdateAsync(string id, Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($id: UUID!, $input: UpdateTransformationInput!) {{ updateTransformation(id: $id, input: $input) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id, ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Transformation>(GetField(data, "updateTransformation"));
    }

    /// <summary>Delete a transformation.</summary>
    /// <param name="id">Transformation UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteTransformation(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteTransformation").GetBoolean();
    }

    /// <summary>Dry-run a transformation against a sample payload.</summary>
    /// <param name="input">Test input payload.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The transformation result.</returns>
    public async Task<TransformTestResult> TestAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = "mutation($input: TestTransformationInput!) { testTransformation(input: $input) { success output error durationMs } }";
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<TransformTestResult>(GetField(data, "testTransformation"));
    }
}
