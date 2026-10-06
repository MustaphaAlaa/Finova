using System.Runtime.CompilerServices;

namespace BankSimulator.Domain;

public class Transaction
{
    public Guid Id { get; set; }

    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<TransactionMetadata> TransactionMetadata { get; set; }
}
