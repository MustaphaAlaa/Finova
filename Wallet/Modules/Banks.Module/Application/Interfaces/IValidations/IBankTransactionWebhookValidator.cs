using Banks.Application.Dtos;

namespace Banks.Application.Interfaces.IValidations;

public interface IBankTransactionWebhookValidator
{
    Task ValidateAndEnsureNewDeliveryAsync(BankTransactionWebhookRequest request, CancellationToken ct);
}