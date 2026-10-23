namespace Banks.Contracts.Enums;


/// <summary>
/// Defines the processing states of an inbound webhook message.
/// </summary>
public enum InboundTransactionWebhookOutboxMessageStatus
{
    /// <summary>
    /// The message has been persisted and is waiting to be processed.
    /// </summary>
    Pending,

    /// <summary>
    /// The message is currently being processed by a background worker.
    /// </summary>
    Processing,

    /// <summary>
    /// The message has been processed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Processing has failed permanently or the retry policy has been exhausted.
    /// </summary>
    Failed
}