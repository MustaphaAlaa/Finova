using System.ComponentModel.DataAnnotations.Schema;

namespace Banks.Domain.Entities;

public class BankAccountBalance
{
    public Guid Id { get; set; }

    [ForeignKey("BankAccount")]
    public required Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; }
    public decimal Balance { get; set; }
    public DateTime LastUpdate { get; set; }
}
