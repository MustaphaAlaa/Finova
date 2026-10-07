namespace BankSimulator.Application;

public sealed record CreateTransaction(Guid BankAccountId, decimal Amount, string Reference);

public static class WebhookEventTypes
{
    public const string TransactionCreated = "transaction.created";
}

public sealed record TransactionCreatedEvent(
    Guid TransactionId,
    Guid ClientId,
    Guid BankAccountId,
    decimal Amount,
    string Reference,
    DateTime Date);
