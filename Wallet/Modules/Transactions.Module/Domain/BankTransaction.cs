using Banks.Domain.Entities;

namespace Transactions.Domain;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

public class BankTransaction
{
    public Guid Id { get; set; }

    [ForeignKey("BankAccount")]
    [Description("Bank Account Sends The Money")]
    public Guid Source { get; set; }

    [ForeignKey("BankAccount")]
    [Description("Bank Account Receive The Money")]
    public Guid Destination { get; set; }

    public BankAccount BankAccount { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime Date { get; set; }


    public Guid BankId { get; init; }

    [Column(TypeName = "jsonb")] public string? Paylod { get; set; }
}