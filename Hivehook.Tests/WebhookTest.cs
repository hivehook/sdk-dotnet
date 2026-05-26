using Hivehook;
using Xunit;

namespace Hivehook.Tests;

public class WebhookTest
{
    private const string Secret = "whsec_test123";
    private const string Payload = "{\"event\":\"test\"}";

    [Fact]
    public void HeaderConstants()
    {
        Assert.Equal("X-Hivehook-Signature", Webhook.HeaderSignature);
        Assert.Equal("X-Hivehook-Timestamp", Webhook.HeaderTimestamp);
        Assert.Equal("X-Hivehook-Message-ID", Webhook.HeaderMessageId);
    }

    [Fact]
    public void SignProducesV1Prefix()
    {
        var sig = Webhook.Sign(Payload, Secret, 1700000000);
        Assert.StartsWith("v1=", sig);
        Assert.Equal(67, sig.Length);
    }

    [Fact]
    public void SignIsDeterministic()
    {
        var sig1 = Webhook.Sign(Payload, Secret, 1700000000);
        var sig2 = Webhook.Sign(Payload, Secret, 1700000000);
        Assert.Equal(sig1, sig2);
    }

    [Fact]
    public void DifferentSecretsProduceDifferentSignatures()
    {
        var sig1 = Webhook.Sign(Payload, Secret, 1700000000);
        var sig2 = Webhook.Sign(Payload, "different", 1700000000);
        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void VerifyValid()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sig = Webhook.Sign(Payload, Secret, ts);
        Assert.True(Webhook.Verify(Payload, Secret, sig, ts, 300));
    }

    [Fact]
    public void RejectWrongSecret()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sig = Webhook.Sign(Payload, Secret, ts);
        Assert.False(Webhook.Verify(Payload, "wrong", sig, ts, 300));
    }

    [Fact]
    public void RejectExpired()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 600;
        var sig = Webhook.Sign(Payload, Secret, ts);
        Assert.False(Webhook.Verify(Payload, Secret, sig, ts, 300));
    }

    [Fact]
    public void NullToleranceSkipsTimestampCheck()
    {
        // tolerance == null -> skip timestamp check entirely.
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 600;
        var sig = Webhook.Sign(Payload, Secret, ts);
        Assert.True(Webhook.Verify(Payload, Secret, sig, ts));
    }

    [Fact]
    public void ZeroToleranceIsStrict()
    {
        // tolerance == 0 -> require now == timestamp exactly. A 60s-old timestamp must fail.
        var stale = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 60;
        var sig = Webhook.Sign(Payload, Secret, stale);
        Assert.False(Webhook.Verify(Payload, Secret, sig, stale, 0));
    }

    [Fact]
    public void ZeroToleranceAcceptsExactSecond()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sig = Webhook.Sign(Payload, Secret, ts);
        Assert.True(Webhook.Verify(Payload, Secret, sig, ts, 0));
    }

    [Fact]
    public void NullSignatureReturnsFalse()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Assert.False(Webhook.Verify(Payload, Secret, null, ts, 300));
        Assert.False(Webhook.VerifyWithRotation(Payload, "primary", "secondary", null, ts, 300));
    }

    [Fact]
    public void MultiSchemeSignatureExtractsV1()
    {
        // Header carries both a timestamp element and v1=..., should still verify.
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var v1 = Webhook.Sign(Payload, Secret, ts);
        var header = $"t={ts},{v1}";
        Assert.True(Webhook.Verify(Payload, Secret, header, ts, 300));
    }

    [Fact]
    public void MultiSchemeSignatureV1NotFirst()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var v1 = Webhook.Sign(Payload, Secret, ts);
        var header = $"v0=ignored,{v1},extra=x";
        Assert.True(Webhook.Verify(Payload, Secret, header, ts, 300));
    }

    [Fact]
    public void SignatureWithoutV1ReturnsFalse()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Assert.False(Webhook.Verify(Payload, Secret, "t=123,v2=abc", ts, 300));
    }

    [Fact]
    public void RotationPrimary()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sig = Webhook.Sign(Payload, "primary", ts);
        Assert.True(Webhook.VerifyWithRotation(Payload, "primary", "secondary", sig, ts, 300));
    }

    [Fact]
    public void RotationSecondary()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sig = Webhook.Sign(Payload, "secondary", ts);
        Assert.True(Webhook.VerifyWithRotation(Payload, "primary", "secondary", sig, ts, 300));
    }

    [Fact]
    public void RotationReject()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Assert.False(Webhook.VerifyWithRotation(Payload, "primary", "secondary", "v1=bad", ts, 300));
    }
}
