using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>Manage user bookmarks on events.</summary>
public sealed class BookmarkService : BaseService
{
    private const string Fragment = "id eventId name notes createdAt";

    /// <summary>Initialize the service.</summary>
    /// <param name="transport">Shared GraphQL transport.</param>
    public BookmarkService(GraphQLTransport transport) : base(transport) { }

    /// <summary>List bookmarks.</summary>
    /// <param name="options">Optional filter/pagination variables.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A paginated list of bookmarks.</returns>
    public async Task<ListResult<Bookmark>> ListAsync(Dictionary<string, object?>? options = null, CancellationToken cancellationToken = default)
    {
        var query = $@"query($eventId: UUID, $search: String, $limit: Int, $offset: Int, $after: String, $first: Int) {{
            bookmarks(eventId: $eventId, search: $search, limit: $limit, offset: $offset, after: $after, first: $first) {{
                nodes {{ {Fragment} }}
                pageInfo {{ total limit offset endCursor hasNextPage }}
            }}
        }}";
        var vars = BuildVariables(options, "eventId", "search", "limit", "offset", "after", "first");
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return DeserializeList<Bookmark>(GetField(data, "bookmarks"));
    }

    /// <summary>Fetch a single bookmark by id.</summary>
    /// <param name="id">Bookmark UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The bookmark, or <c>null</c> if not found.</returns>
    public async Task<Bookmark?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = $"query($id: UUID!) {{ bookmark(id: $id) {{ {Fragment} }} }}";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return DeserializeNullable<Bookmark>(GetField(data, "bookmark"));
    }

    /// <summary>Create a bookmark on an event.</summary>
    /// <param name="eventId">Event UUID.</param>
    /// <param name="name">Optional name for the bookmark.</param>
    /// <param name="notes">Optional free-form notes.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created bookmark.</returns>
    public async Task<Bookmark> CreateAsync(string eventId, string? name = null, string? notes = null, CancellationToken cancellationToken = default)
    {
        var query = $"mutation($eventId: UUID!, $name: String, $notes: String) {{ createBookmark(eventId: $eventId, name: $name, notes: $notes) {{ {Fragment} }} }}";
        var vars = new Dictionary<string, object?> { ["eventId"] = eventId };
        if (name != null) vars["name"] = name;
        if (notes != null) vars["notes"] = notes;
        var data = await Transport.ExecuteAsync(query, vars, cancellationToken).ConfigureAwait(false);
        return Deserialize<Bookmark>(GetField(data, "createBookmark"));
    }

    /// <summary>Delete a bookmark.</summary>
    /// <param name="id">Bookmark UUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = "mutation($id: UUID!) { deleteBookmark(id: $id) }";
        var data = await Transport.ExecuteAsync(query, new() { ["id"] = id }, cancellationToken).ConfigureAwait(false);
        return GetField(data, "deleteBookmark").GetBoolean();
    }
}
