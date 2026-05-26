using Hivehook;
using Hivehook.Resources;
using Xunit;

namespace Hivehook.Tests;

public class ClientTest
{
    [Fact]
    public void ClientHasAllServices()
    {
        var client = new HivehookClient();
        Assert.IsType<SourceService>(client.Sources);
        Assert.IsType<DestinationService>(client.Destinations);
        Assert.IsType<SubscriptionService>(client.Subscriptions);
        Assert.IsType<EventService>(client.Events);
        Assert.IsType<DeliveryService>(client.Deliveries);
        Assert.IsType<DLQService>(client.DLQ);
        Assert.IsType<APIKeyService>(client.APIKeys);
        Assert.IsType<AlertRuleService>(client.AlertRules);
        Assert.IsType<BookmarkService>(client.Bookmarks);
        Assert.IsType<EventTypeSchemaService>(client.EventTypeSchemas);
        Assert.IsType<ApplicationService>(client.Applications);
        Assert.IsType<EndpointService>(client.Endpoints);
        Assert.IsType<MessageService>(client.Messages);
        Assert.IsType<OutboundDeliveryService>(client.OutboundDeliveries);
        Assert.IsType<OutboundDLQService>(client.OutboundDLQ);
        Assert.IsType<StatusService>(client.Status);
        Assert.IsType<TransformationService>(client.Transformations);
        Assert.IsType<PortalService>(client.Portal);
        Assert.IsType<StreamService>(client.Streams);
        Assert.IsType<StreamConsumerService>(client.StreamConsumers);
        Assert.IsType<StreamSinkService>(client.StreamSinks);
        Assert.NotNull(client.Organizations);
        Assert.NotNull(client.Users);
        Assert.NotNull(client.AuditLogs);
    }
}
