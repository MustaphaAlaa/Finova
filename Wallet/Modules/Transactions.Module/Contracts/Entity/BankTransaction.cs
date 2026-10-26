using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Banks.Contracts.Entities;


namespace Transactions.Contracts.Entities;


public class BankTransaction
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }

    [ForeignKey("BankAccount")]
    [Description("Bank Account Sends The Money")]
    public Guid Source { get; set; }
    public Guid WebhookDeliveryId { get; set; }

    [ForeignKey("BankAccount")]
    [Description("Bank Account Receive The Money")]
    public Guid Destination { get; set; }
    public Currencies Currency { get; set; }

    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public Guid BankId { get; set; }

    [Column(TypeName = "jsonb")] public JsonDocument? Metadata { get; set; }
}

