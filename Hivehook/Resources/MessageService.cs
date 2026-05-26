using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage outbound webhook messages.</summary>
public sealed class MessageService : BaseService
{
    private const string Fragment = "id applicationId eventType payload idempotencyKey status createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public MessageService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List messages.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of messages.</returns>
    public async Task<ListResult<Message>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($applicationId: UUID, $eventType: String, $status: MessageStatus, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            messages(applicationId: $applicationId, eventType: $eventType, status: $status, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "applicationId", "eventType", "status", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<Message>(GetField(data, "messages"));
    }

    /// <summary>Fetch a single message by id.</summary>
    /// <param name="id">Message UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The message, or <c>null</c> if not found.</returns>
    public async Task<Message?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ message(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<Message>(GetField(data, "message"));
    }

    /// <summary>Send a message. Encodes a string <c>payload</c> as base64 if present.</summary>
    /// <param name="input">Send input payload matching <c>SendMessageInput</c>.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The sent message.</returns>
    public async Task<Message> SendAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: SendMessageInput!) {{ sendMessage(input: $input) {{ {Fragment} }} }}";
        if (input.TryGetValue("payload", out var payload) && payload is string payloadStr)
            input["payload"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadStr));
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Message>(GetField(data, "sendMessage"));
    }

    /// <summary>Broadcast a message to every endpoint of an application.</summary>
    /// <param name="input">Broadcast input payload matching <c>BroadcastMessageInput</c>.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The broadcast message.</returns>
    public async Task<Message> BroadcastAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($input: BroadcastMessageInput!) {{ broadcastMessage(input: $input) {{ {Fragment} }} }}";
        if (input.TryGetValue("payload", out var payload) && payload is string payloadStr)
            input["payload"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadStr));
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<Message>(GetField(data, "broadcastMessage"));
    }

    /// <summary>Send a dynamic message (caller-specified URL, bypassing endpoints).</summary>
    /// <param name="input">Send input payload matching <c>SendDynamicMessageInput</c>.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The scheduled outbound delivery.</returns>
    public async Task<OutboundDelivery> SendDynamicAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        const string deliveryFragment = "id messageId endpointId status attempts maxAttempts nextAttemptAt createdAt";
        var query = $"mutation($input: SendDynamicMessageInput!) {{ sendDynamicMessage(input: $input) {{ {deliveryFragment} }} }}";
        if (input.TryGetValue("payload", out var payload) && payload is string payloadStr)
            input["payload"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadStr));
        var data = await Transport.ExecuteAsync(query, new() { ["input"] = input }, cancellationToken).ConfigureAwait(false);
        return Deserialize<OutboundDelivery>(GetField(data, "sendDynamicMessage"));
    }
}
