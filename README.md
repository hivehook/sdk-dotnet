# Hivehook (.NET)

Official .NET client for [Hivehook](https://hivehook.com), webhook infrastructure for modern teams (inbound and outbound).

Latest release: **0.1.1** on [NuGet](https://www.nuget.org/packages/Hivehook).

## Install

```bash
dotnet add package Hivehook
```

## Quick start

```csharp
using Hivehook;

var client = new HivehookClient(
    baseUrl: "http://localhost:8080",
    apiKey: Environment.GetEnvironmentVariable("HIVEHOOK_API_KEY")
);

var source = await client.Sources.CreateAsync(new Dictionary<string, object?>
{
    ["name"] = "Stripe production",
    ["slug"] = "stripe-prod",
    ["providerType"] = "stripe",
    ["verifyConfig"] = new Dictionary<string, object?> { ["secret"] = "whsec_..." },
});

Console.WriteLine(
    $"created source {source.Id}. POST webhooks to /ingest/{source.Slug}"
);
```

## Webhook signature verification

```csharp
using Hivehook;

string signature = Request.Headers["X-Hivehook-Signature"];
long timestamp = long.Parse(Request.Headers["X-Hivehook-Timestamp"]);
bool ok = Webhook.Verify(body, "your-signing-secret", signature, timestamp, 300);
```

## Requirements

- .NET 8.0 and .NET Standard 2.0

## Documentation

See the full reference at [hivehook.com/docs](https://hivehook.com/docs).

## License

MIT. See [LICENSE](LICENSE).
