using System;
using System.Security.Cryptography;
using System.Text;

namespace Hivehook;

/// <summary>Helpers for signing and verifying inbound webhook payloads.</summary>
public static class Webhook
{
    /// <summary>Header carrying the v1=hex(HMAC-SHA256(secret, body)) signature.</summary>
    public const string HeaderSignature = "X-Hivehook-Signature";
    /// <summary>Header carrying the unix-second timestamp included in the signature.</summary>
    public const string HeaderTimestamp = "X-Hivehook-Timestamp";
    /// <summary>Header carrying the message id for idempotency.</summary>
    public const string HeaderMessageId = "X-Hivehook-Message-ID";

    /// <summary>Compute the v1 signature for a payload.</summary>
    /// <param name="payload">Raw body string.</param>
    /// <param name="secret">Signing secret.</param>
    /// <param name="timestamp">Unix timestamp in seconds.</param>
    /// <returns>The signature with <c>v1=</c> prefix.</returns>
    public static string Sign(string payload, string secret, long timestamp)
    {
        var message = $"{timestamp}.{payload}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(message);
#if NETSTANDARD2_0
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(messageBytes);
        return "v1=" + ToHexLower(hash);
#else
        var hash = HMACSHA256.HashData(keyBytes, messageBytes);
        return $"v1={Convert.ToHexString(hash).ToLowerInvariant()}";
#endif
    }

    /// <summary>Verify a payload against a signature.</summary>
    /// <remarks>
    /// Tolerance semantics:
    /// <list type="bullet">
    ///   <item><c>null</c>: skip the timestamp check entirely.</item>
    ///   <item><c>0</c>: strict mode: require exact-second equality (<c>now == timestamp</c>).</item>
    ///   <item>positive: enforce <c>|now - timestamp| &lt;= toleranceSeconds</c>.</item>
    /// </list>
    /// </remarks>
    /// <param name="payload">Raw body string.</param>
    /// <param name="secret">Signing secret.</param>
    /// <param name="signature">Signature header value. May be <c>null</c> (returns false).
    /// Supports multi-scheme headers like <c>t=...,v1=...</c> by selecting the <c>v1=</c> element.</param>
    /// <param name="timestamp">Unix timestamp from header.</param>
    /// <param name="toleranceSeconds">Max age in seconds. See remarks for semantics.</param>
    /// <returns><c>true</c> when the signature is valid and timely.</returns>
    public static bool Verify(string payload, string secret, string? signature, long timestamp, int? toleranceSeconds = null)
    {
        if (signature == null) return false;

        if (toleranceSeconds.HasValue)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (toleranceSeconds.Value == 0)
            {
                if (now != timestamp) return false;
            }
            else
            {
                var age = Math.Abs(now - timestamp);
                if (age > toleranceSeconds.Value) return false;
            }
        }

        var v1 = ExtractV1(signature);
        if (v1 == null) return false;

        var expected = Sign(payload, secret, timestamp);
        return FixedTimeEqualsAscii(expected, v1);
    }

    /// <summary>Verify against a primary + optional rotated secondary secret.</summary>
    /// <param name="payload">Raw body string.</param>
    /// <param name="primary">Primary signing secret.</param>
    /// <param name="secondary">Optional secondary signing secret.</param>
    /// <param name="signature">Signature header value (may be <c>null</c>).</param>
    /// <param name="timestamp">Unix timestamp from header.</param>
    /// <param name="toleranceSeconds">Max age in seconds. See <see cref="Verify"/> for semantics.</param>
    /// <returns><c>true</c> when either secret produces a valid signature.</returns>
    public static bool VerifyWithRotation(string payload, string primary, string? secondary, string? signature, long timestamp, int? toleranceSeconds = null)
    {
        if (Verify(payload, primary, signature, timestamp, toleranceSeconds))
            return true;
        if (secondary != null)
            return Verify(payload, secondary, signature, timestamp, toleranceSeconds);
        return false;
    }

    /// <summary>
    /// Extract the <c>v1=...</c> element from a (possibly multi-scheme) signature header.
    /// Accepts <c>v1=...</c> and <c>t=...,v1=...</c>-style values; returns the full
    /// <c>v1=&lt;hex&gt;</c> token (including prefix) or <c>null</c> if missing.
    /// </summary>
    private static string? ExtractV1(string signature)
    {
        if (string.IsNullOrEmpty(signature)) return null;
        var parts = signature.Split(',');
        foreach (var raw in parts)
        {
            var p = raw.Trim();
            if (p.StartsWith("v1=", StringComparison.Ordinal))
                return p;
        }
        return null;
    }

    private static bool FixedTimeEqualsAscii(string a, string b)
    {
        var ab = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
#if NETSTANDARD2_0
        if (ab.Length != bb.Length) return false;
        var diff = 0;
        for (var i = 0; i < ab.Length; i++)
            diff |= ab[i] ^ bb[i];
        return diff == 0;
#else
        return CryptographicOperations.FixedTimeEquals(ab, bb);
#endif
    }

#if NETSTANDARD2_0
    private static string ToHexLower(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
#endif
}
