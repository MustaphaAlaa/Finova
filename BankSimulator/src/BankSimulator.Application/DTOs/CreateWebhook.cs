namespace BankSimulator.Application;

public sealed record CreateTransactionWebhookRequest(Guid ClientId, string EventType, string WebhookUrl);
