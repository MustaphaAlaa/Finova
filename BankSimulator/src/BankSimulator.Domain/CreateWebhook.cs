namespace BankSimulator.Domain;

public sealed record CreateWebhook(string EventType, string WebhookUrl);
