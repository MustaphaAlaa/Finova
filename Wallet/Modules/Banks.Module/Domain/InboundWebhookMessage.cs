using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Banks.Domain.Entities;

namespace Banks.Domain;

/// <summary>
/// Stores incoming bank webhook notifications for durable, asynchronous processing.
/// </summary>
/// <remarks>
/// The original payload is persisted before processing begins, allowing failed or
/// interrupted processing attempts to be retried without requiring the bank to
/// resend the notification.
/// </remarks>
[Description("Stores incoming bank webhook notifications for durable, asynchronous processing.")]
public sealed class InboundWebhookMessage
{
    /// <summary>
    /// Gets or sets the internal unique identifier of the stored message.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the unique identifier assigned to this webhook delivery.
    /// Used to identify repeated deliveries when the sender preserves the same ID.
    /// </summary>
    public Guid WebhookDeliveryId { get; init; }

    /// <summary>
    /// Gets or sets the type of event represented by the webhook notification.
    /// </summary>
    public InboundWebhookEventType EventType { get; init; }

    /// <summary>
    /// Gets or sets the original webhook payload received from the bank.
    /// The payload is stored before parsing or business processing.
    /// </summary>
    public required string Payload { get; init; }

    public required long PayloadSizeBytes { get; init; }
    public DateTime? LockedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the bank that originated the webhook notification.
    /// </summary>
    [ForeignKey("Bank")]
    public Guid BankId { get; init; }

    public required Bank Bank { get; init; }

    /// <summary>
    /// Gets or sets the UTC date and time when the webhook was received and persisted.
    /// </summary>
    public DateTime ReceivedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the earliest UTC date and time when the message is eligible
    /// for another processing attempt.
    /// </summary>
    public DateTime? NextAttemptAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the UTC date and time when published successfully.
    /// Null if the message has not been published.
    /// </summary>
    public DateTime? PublishedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the number of processing attempts made for this message.
    /// </summary>
    public int AttemptCount { get; init; }

    /// <summary>
    /// Gets or sets the error details from the most recent failed processing attempt.
    /// Null if no processing error has occurred.
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    /// Gets or sets the current processing status of the webhook message.
    /// </summary>
    public InboundTransactionWebhookOutboxMessageStatus Status { get; init; }
}