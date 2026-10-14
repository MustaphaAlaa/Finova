namespace Finova.Modules.Banks.Domain.Entities;

public class Currency
{
    public Guid Id { get; set; }
    public required Currencies Code { get; set; }
    public required string Name { get; set; }
    public IList<BankAccount> BankAccounts;
}

public class BankAccountBalance
{
    public Guid BankAccountId { get; set; }
    public decimal Balance { get; set; }
    public DateTime LastUpdate { get; set; }
}

// public class LedgerEntry
// {
//     public Guid Id { get; set; }
//     public Guid TransactionId { get; set; }
//     public Transaction Transaction { get; set; }
//     public Guid BankAccountId { get; set; }
//     public BankAccount BankAccount { get; set; }
//     public decimal Amount { get; set; }
// }
