using Banks.Application.Dtos;

namespace Banks.Application.Interfaces.IValidations;

/// <summary>
/// Validates incoming webhook deliveries by checking required request fields,
/// verifying that the bank exists and is contracted, and rejecting duplicate
/// webhook delivery IDs.
/// </summary>
public interface IWebhookDeliveryValidator
{
    Task ValidateNewDeliveryAsync(BankTransactionWebhookRequest request, CancellationToken ct);
}