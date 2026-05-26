using System.Text.Json;
using Hivehook;
using Hivehook.Types;
using Xunit;

namespace Hivehook.Tests;

public class TypesTest
{
    [Fact]
    public void SourceDeserializesCamelCaseFields()
    {
        const string json = """
        {
          "id": "src-1",
          "name": "Stripe",
          "slug": "stripe",
          "providerType": "stripe",
          "rateLimitRps": 100,
          "spikeProtection": true,
          "maxIngestRps": 1000,
          "createdAt": "2026-01-01T00:00:00Z"
        }
        """;
        var src = JsonSerializer.Deserialize<Source>(json, GraphQLTransport.JsonOptions);
        Assert.NotNull(src);
        Assert.Equal("src-1", src!.Id);
        Assert.Equal("stripe", src.ProviderType);
        Assert.Equal(100, src.RateLimitRps);
        Assert.True(src.SpikeProtection);
        Assert.Equal(1000, src.MaxIngestRps);
    }

    [Fact]
    public void DestinationDeserializesNestedTypes()
    {
        const string json = """
        {
          "id": "dst-1",
          "name": "test",
          "url": "https://example.com",
          "retryPolicy": { "maxAttempts": 5, "initialDelay": "1s", "maxDelay": "30s", "backoffFactor": 2.0 },
          "oauth2Config": { "tokenUrl": "https://auth", "clientId": "abc", "scopes": ["read", "write"] },
          "healthConfig": { "windowHours": 24, "disableBelow": 0.5 }
        }
        """;
        var dst = JsonSerializer.Deserialize<Destination>(json, GraphQLTransport.JsonOptions);
        Assert.NotNull(dst);
        Assert.NotNull(dst!.RetryPolicy);
        Assert.Equal(5, dst.RetryPolicy!.MaxAttempts);
        Assert.Equal(2.0, dst.RetryPolicy.BackoffFactor);
        Assert.NotNull(dst.Oauth2Config);
        Assert.Equal("abc", dst.Oauth2Config!.ClientId);
        Assert.NotNull(dst.Oauth2Config.Scopes);
        Assert.Equal(2, dst.Oauth2Config!.Scopes!.Count);
        Assert.NotNull(dst.HealthConfig);
        Assert.Equal(24, dst.HealthConfig!.WindowHours);
    }

    [Fact]
    public void ListResultDeserializesNodesAndPageInfo()
    {
        const string json = """
        {
          "nodes": [
            { "id": "a", "name": "n1" },
            { "id": "b", "name": "n2" }
          ],
          "pageInfo": { "total": 2, "limit": 10, "offset": 0, "hasNextPage": false }
        }
        """;
        var result = JsonSerializer.Deserialize<ListResult<Application>>(json, GraphQLTransport.JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result!.Nodes.Count);
        Assert.Equal("a", result.Nodes[0].Id);
        Assert.Equal("n2", result.Nodes[1].Name);
        Assert.Equal(2, result.PageInfo.Total);
        Assert.False(result.PageInfo.HasNextPage);
    }

    [Fact]
    public void SystemStatusDeserializesScalarCounters()
    {
        const string json = """
        {
          "status": "ok",
          "dlqSize": 3,
          "queueDepth": 12,
          "activeWorkers": 4,
          "totalWorkers": 8,
          "uptime": 3600,
          "version": "0.1.0",
          "eventsTotal": 1000000,
          "deliveriesPending": 5
        }
        """;
        var s = JsonSerializer.Deserialize<SystemStatus>(json, GraphQLTransport.JsonOptions);
        Assert.NotNull(s);
        Assert.Equal("ok", s!.Status);
        Assert.Equal(3, s.DlqSize);
        Assert.Equal(1000000L, s.EventsTotal);
        Assert.Equal(5L, s.DeliveriesPending);
    }
}
