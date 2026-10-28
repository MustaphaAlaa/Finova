namespace Banks.Application.Dtos.Unused_yet;

public sealed record CreateTransactionWebhookRequest(Guid ClientId, string EventType, string WebhookUrl);
