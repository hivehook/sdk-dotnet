using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Hivehook.Exceptions;

/// <summary>Base exception for all Hivehook SDK errors.</summary>
public class HivehookException : Exception
{
    /// <summary>Create a Hivehook exception.</summary>
    /// <param name="message">Human-readable message.</param>
    public HivehookException(string message) : base(message) { }

    /// <summary>Create a Hivehook exception with an inner cause.</summary>
    /// <param name="message">Human-readable message.</param>
    /// <param name="inner">Underlying exception.</param>
    public HivehookException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>The Hivehook API returned a non-success response.</summary>
public class ApiException : HivehookException
{
    /// <summary>HTTP status code returned by the server, if available.</summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Raw <c>extensions</c> map from the GraphQL error envelope, if present.
    /// Preserves server-provided diagnostic fields like <c>code</c>, <c>field</c>, or
    /// rate-limit metadata so callers can branch on them.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement>? Extensions { get; init; }

    /// <summary>Create an API exception.</summary>
    /// <param name="message">Server-supplied error message.</param>
    /// <param name="statusCode">Optional HTTP status code.</param>
    public ApiException(string message, int? statusCode = null) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>The requested resource was not found.</summary>
public sealed class NotFoundException : ApiException
{
    /// <summary>Create a not-found exception.</summary>
    /// <param name="message">Server-supplied error message.</param>
    /// <param name="statusCode">Optional HTTP status code.</param>
    public NotFoundException(string message, int? statusCode = null) : base(message, statusCode) { }
}

/// <summary>The request conflicted with the server state (e.g. duplicate key).</summary>
public sealed class ConflictException : ApiException
{
    /// <summary>Create a conflict exception.</summary>
    /// <param name="message">Server-supplied error message.</param>
    /// <param name="statusCode">Optional HTTP status code.</param>
    public ConflictException(string message, int? statusCode = null) : base(message, statusCode) { }
}

/// <summary>Authentication failed (missing or invalid API key).</summary>
public sealed class AuthException : ApiException
{
    /// <summary>Create an auth exception.</summary>
    /// <param name="message">Server-supplied error message.</param>
    /// <param name="statusCode">HTTP status code (defaults to 401).</param>
    public AuthException(string message, int? statusCode = 401) : base(message, statusCode) { }
}

/// <summary>The request failed server-side validation.</summary>
public sealed class ValidationException : ApiException
{
    /// <summary>Create a validation exception.</summary>
    /// <param name="message">Server-supplied error message.</param>
    /// <param name="statusCode">Optional HTTP status code.</param>
    public ValidationException(string message, int? statusCode = null) : base(message, statusCode) { }
}

/// <summary>The server rejected the request because the caller hit a rate limit (HTTP 429).</summary>
public sealed class RateLimitException : ApiException
{
    /// <summary>Value parsed from the <c>Retry-After</c> response header, if present.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>Create a rate-limit exception.</summary>
    /// <param name="message">Server-supplied error message.</param>
    /// <param name="retryAfter">Optional <c>Retry-After</c> duration.</param>
    /// <param name="statusCode">HTTP status code (defaults to 429).</param>
    public RateLimitException(string message, TimeSpan? retryAfter = null, int? statusCode = 429) : base(message, statusCode)
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>The server returned a 5xx error.</summary>
public sealed class ServerException : ApiException
{
    /// <summary>Create a server exception.</summary>
    /// <param name="message">Server-supplied error message.</param>
    /// <param name="statusCode">HTTP status code (5xx).</param>
    public ServerException(string message, int? statusCode = null) : base(message, statusCode) { }
}
