namespace Banks.Application.Dtos.Unused_yet;

public sealed record IncomingTransactionNotificationWebhook(
    string EventId,
    List<MetadataPool> MetadataPools
);