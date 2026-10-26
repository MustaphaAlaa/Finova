using System.Text.Json;
using Banks.Contrancts.Enums; 

namespace Banks.Application.Dtos;

public sealed record BankTransactionWebhookRequest(
    Guid WebhookDeliveryId,
    ContractedBanks BankCode,
    JsonDocument Payload
);