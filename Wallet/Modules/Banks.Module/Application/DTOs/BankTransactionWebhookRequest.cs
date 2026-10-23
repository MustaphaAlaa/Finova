using System.Text.Json;
using Banks.Domain;
using Banks.Domain.Entities;

namespace Banks.Application.Dtos;

public sealed record BankTransactionWebhookRequest(
    Guid WebhookDeliveryId,
    ContractedBanks BankCode,
    JsonDocument Payload
);