namespace Transaction.Contracts.Enums;

public enum TransactionMessageProcessingStatus
{
    Pending,
    Processing,
    Processed,
    Rejected,
    Failed
}