using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Query gateway-wide status counters.</summary>
public sealed class StatusService : BaseService
{
    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public StatusService(GraphQLTransport transport) : base(transport) { }

    /// <summary>Fetch the current system status snapshot.</summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Aggregate gateway status.</returns>
    public async Task<SystemStatus> GetAsync(CancellationToken cancellationToken = default)
    {
        var query = @"query {
            status {
                status dlqSize outboundDlqSize queueDepth activeWorkers totalWorkers uptime version
                sourcesTotal destinationsTotal subscriptionsTotal eventsTotal eventsFailed
                deliveriesTotal deliveriesPending deliveriesDelivered
                messagesTotal outboundDeliveriesTotal outboundDeliveriesPending outboundDeliveriesFailed
            }
        }";
        var data = await Transport.ExecuteAsync(query, null, cancellationToken).ConfigureAwait(false);
        return Deserialize<SystemStatus>(GetField(data, "status"));
    }
}
