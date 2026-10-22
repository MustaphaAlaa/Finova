using Banks.Domain.Entities;

namespace Banks.Application.Dtos;

public sealed record BankTransactionWebhookRequest(
    Guid WebhookDeliveryId,
    ContractedBanks BankCode,
    string Payload
);