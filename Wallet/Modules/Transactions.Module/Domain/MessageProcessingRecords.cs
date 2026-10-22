using Transaction.Domain.Enums;

namespace Transaction.Domain;

public class MessageProcessingRecords
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string? Reason { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
    public TransactionMessageProcessingStatus Status { get; set; }
}