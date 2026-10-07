namespace BankSimulator.Application;

public sealed record TransactionCreatedEvent(
    Guid TransactionId,
    Guid ClientId,
    Guid BankAccountId,
    decimal Amount,
    string Reference,
    DateTime Date);
