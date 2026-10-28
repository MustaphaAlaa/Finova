namespace Banks.Application.Dtos.Unused_yet;

public sealed record CreateTransaction(Guid BankAccountId, decimal Amount, string Reference);
