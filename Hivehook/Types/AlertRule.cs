using System.Text.Json.Serialization;

namespace Hivehook.Types;

/// <summary>
/// An alerting rule that fires on a metric or condition.
/// </summary>
public sealed record AlertRule(
    [property: JsonPropertyName("id")] string Id = "",
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("conditionType")] string ConditionType = "",
    [property: JsonPropertyName("threshold")] int Threshold = 0,
    [property: JsonPropertyName("webhookUrl")] string WebhookUrl = "",
    [property: JsonPropertyName("channel")] string Channel = "WEBHOOK",
    [property: JsonPropertyName("emailConfig")] EmailAlertConfig? EmailConfig = null,
    [property: JsonPropertyName("slackConfig")] SlackAlertConfig? SlackConfig = null,
    [property: JsonPropertyName("cooldown")] string Cooldown = "",
    [property: JsonPropertyName("enabled")] bool Enabled = true,
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);
