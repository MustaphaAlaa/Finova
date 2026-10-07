namespace BankSimulator.Application;

public sealed record CreateTransaction(Guid BankAccountId, decimal Amount, string Reference);
