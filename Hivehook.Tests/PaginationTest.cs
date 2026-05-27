using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hivehook;
using Hivehook.Resources;
using Xunit;

namespace Hivehook.Tests;

public class PaginationTest
{
    private sealed class SeqHandler : DelegatingHandler
    {
        private readonly string[] _bodies;
        private int _i;
        public SeqHandler(params string[] bodies) => _bodies = bodies;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = _bodies[Math.Min(_i, _bodies.Length - 1)];
            _i++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public async Task ListAllAsyncWalksEveryPage()
    {
        var handler = new SeqHandler(
            """{"data":{"sources":{"nodes":[{"id":"a"},{"id":"b"}],"pageInfo":{"hasNextPage":true,"endCursor":"c1"}}}}""",
            """{"data":{"sources":{"nodes":[{"id":"c"}],"pageInfo":{"hasNextPage":false}}}}""");
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://stub.local") };
        var transport = new GraphQLTransport("http://stub.local", apiKey: null, httpClient: http, maxRetries: 2, timeout: TimeSpan.FromSeconds(5));
        var svc = new SourceService(transport);

        var ids = new List<string>();
        await foreach (var s in svc.ListAllAsync())
            ids.Add(s.Id);

        Assert.Equal(new[] { "a", "b", "c" }, ids);
    }
}
