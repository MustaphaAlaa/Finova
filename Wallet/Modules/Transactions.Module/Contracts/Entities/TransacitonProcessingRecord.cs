using System.ComponentModel.DataAnnotations;
using Transaction.Contracts.Enums;

namespace Transaction.Contracts.Entities;

public class TransacitonProcessingRecord
{
    [Key]
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public string? Reason { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
    public TransactionMessageProcessingStatus Status { get; set; }
}
