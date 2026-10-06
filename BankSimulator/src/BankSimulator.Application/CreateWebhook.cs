namespace BankSimulator.Application;

public sealed record CreateWebhook(Guid ClientId, string EventType, string WebhookUrl);
