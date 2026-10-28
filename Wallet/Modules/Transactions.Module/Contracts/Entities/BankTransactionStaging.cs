using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Transaction.Contracts.Entities;

[Table("bank_transaction_stage")]
public class BankTransactionStaging
{
    public Guid Id { get; set; }
    public Guid WebhookDeliveryId { get; set; }
    public Guid TransactionId { get; set; }

    public Guid Source { get; set; }

    public Guid Destination { get; set; }
    public Currencies Currency { get; set; }

    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public DateTime StagedAtUtc { get; set; }

    public DateTime? InsertedAtUtc { get; set; }
    public Guid BankId { get; set; }

    public byte Status { get; set; }
    [Description("0 = Pending, 1 = Processed")]

    [Column(TypeName = "jsonb")] public JsonDocument? Metadata { get; set; }

}

