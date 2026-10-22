namespace Transaction.Domain.Enums;

public enum TransactionMessageProcessingStatus
{
    Pending,
    Processing,
    Processed,
    Rejected,
    Failed
}