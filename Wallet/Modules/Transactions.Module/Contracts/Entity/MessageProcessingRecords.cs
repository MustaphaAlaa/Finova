using System.ComponentModel.DataAnnotations;
using Transaction.Contracts.Enums;

namespace Transactions.Contracts.Entities;

public class MessageProcessingRecord
{
    [Key]
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string? Reason { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
    public TransactionMessageProcessingStatus Status { get; set; }
}
