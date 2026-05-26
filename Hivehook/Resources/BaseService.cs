using System.Collections.Generic;
using System.Text.Json;
using Hivehook.Exceptions;
using Hivehook.Types;

namespace Hivehook.Resources;

/// <summary>
/// Shared base class for Hivehook resource services. Provides variable construction
/// and typed deserialization helpers built on top of <see cref="GraphQLTransport"/>.
/// </summary>
public abstract class BaseService
{
    /// <summary>The transport used to execute GraphQL operations.</summary>
    protected readonly GraphQLTransport Transport;

    /// <summary>Initialize the service with a shared transport.</summary>
    /// <param name="transport">GraphQL transport instance.</param>
    protected BaseService(GraphQLTransport transport)
    {
        Transport = transport;
    }

    /// <summary>
    /// Copy a whitelisted subset of options into a fresh variables dictionary.
    /// </summary>
    /// <param name="options">Caller-supplied option bag (may be null).</param>
    /// <param name="allowed">Keys to copy into the GraphQL variables map.</param>
    /// <returns>A new dictionary containing only the allowed keys present in <paramref name="options"/>.</returns>
    protected static Dictionary<string, object?> BuildVariables(Dictionary<string, object?>? options, params string[] allowed)
    {
        var vars = new Dictionary<string, object?>();
        if (options == null) return vars;
        foreach (var key in allowed)
        {
            if (options.TryGetValue(key, out var value))
                vars[key] = value;
        }
        return vars;
    }

    /// <summary>Look up a top-level GraphQL data field by name.</summary>
    /// <param name="data">Top-level data map returned by <see cref="GraphQLTransport.ExecuteAsync"/>.</param>
    /// <param name="key">Field name to retrieve.</param>
    /// <returns>The <see cref="JsonElement"/> for that field.</returns>
    protected static JsonElement GetField(Dictionary<string, JsonElement> data, string key)
    {
        return data[key];
    }

    /// <summary>Deserialize a JSON element into a typed record using SDK defaults.</summary>
    /// <typeparam name="T">Target record type.</typeparam>
    /// <param name="element">Source JSON element.</param>
    /// <returns>The deserialized record.</returns>
    /// <exception cref="ApiException">Thrown when the element does not deserialize.</exception>
    protected static T Deserialize<T>(JsonElement element)
    {
        var result = JsonSerializer.Deserialize<T>(element.GetRawText(), GraphQLTransport.JsonOptions);
        if (result == null)
            throw new ApiException($"failed to deserialize {typeof(T).Name}");
        return result;
    }

    /// <summary>
    /// Deserialize a nullable JSON element into a typed record, returning <c>null</c> when
    /// the element is JSON null or undefined.
    /// </summary>
    /// <typeparam name="T">Target record type.</typeparam>
    /// <param name="element">Source JSON element.</param>
    /// <returns>The deserialized record or <c>null</c>.</returns>
    protected static T? DeserializeNullable<T>(JsonElement element) where T : class
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
            return null;
        return JsonSerializer.Deserialize<T>(element.GetRawText(), GraphQLTransport.JsonOptions);
    }

    /// <summary>
    /// Deserialize a connection-style payload (with <c>nodes</c> and <c>pageInfo</c>) into a <see cref="ListResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">Node record type.</typeparam>
    /// <param name="element">JSON element pointing at the connection object.</param>
    /// <returns>A typed paginated list.</returns>
    protected static ListResult<T> DeserializeList<T>(JsonElement element)
    {
        var result = JsonSerializer.Deserialize<ListResult<T>>(element.GetRawText(), GraphQLTransport.JsonOptions);
        if (result == null)
            throw new ApiException($"failed to deserialize ListResult<{typeof(T).Name}>");
        return result;
    }
}
