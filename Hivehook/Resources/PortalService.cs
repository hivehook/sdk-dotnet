using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Generate tokens for the embeddable customer portal.</summary>
public sealed class PortalService : BaseService
{
    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public PortalService(GraphQLTransport transport) : base(transport) { }

    /// <summary>Generate a portal token scoped to a single application.</summary>
    /// <param name="applicationId">Application UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The portal token with its expiry.</returns>
    public async Task<PortalToken> GenerateTokenAsync(string applicationId, CancellationToken cancellationToken = default)
    {
        var query = "mutation($applicationId: UUID!) { generatePortalToken(applicationId: $applicationId) { token expiresAt } }";
        var data = await Transport.ExecuteAsync(query, new() { ["applicationId"] = applicationId }, cancellationToken).ConfigureAwait(false);
        return Deserialize<PortalToken>(GetField(data, "generatePortalToken"));
    }
}
