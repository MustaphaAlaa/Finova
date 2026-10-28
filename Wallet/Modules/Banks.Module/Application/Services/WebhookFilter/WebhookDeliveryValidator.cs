using System.ComponentModel.DataAnnotations;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IRepositories;
using Banks.Application.Interfaces.IValidations;
using Banks.Contracts.Interfaces;
using Microsoft.Extensions.Logging;

namespace Banks.Application.Services.WebhookFilter;

/// <inheritdoc/>
/// <param name="bankRepository"><see cref="IBankRepository"/></param>
/// <param name="outboxWriter"><see cref="IBankOutboxWriter"/></param>
/// <param name="logger">
/// The logger used to log messages for <see cref="WebhookDeliveryValidator"/>.
/// </param>
public class WebhookDeliveryValidator(
    IBankRepository bankRepository,
    IBankOutboxWriter outboxWriter,
    ILogger<WebhookDeliveryValidator> logger
) : IWebhookDeliveryValidator
{
    public async Task ValidateNewDeliveryAsync(BankTransactionWebhookRequest request, CancellationToken ct)
    {
        ValidateRequiredFields(request);

        await ValidateBankAsync(request, ct);

        var deliveryAlreadyExists = await outboxWriter.ExistsByDeliveryIdAsync(request.WebhookDeliveryId, ct);

        if (deliveryAlreadyExists)
        {
            logger.LogWarning("Transaction Webhook Request is already exist in outbox table");
            throw new ValidationException("Transaction Webhook Request is already sent to us.");
        }
    }

    void ValidateRequiredFields(BankTransactionWebhookRequest request)
    {
        if (request.WebhookDeliveryId == Guid.Empty)
        {
            logger.LogWarning("Webhook DeliveryId is empty");
            throw new ValidationException("Webhook deliveryId is empty");
        }

        if (request.Payload is null || string.IsNullOrWhiteSpace(request.Payload.ToString()))
        {
            logger.LogWarning("Payload is empty");
            throw new ValidationException("Payload is empty");
        }

        if (!Enum.IsDefined(request.BankCode))
        {
            logger.LogWarning("Bank code is invalid");
            throw new ValidationException("Bank Code is invalid");
        }
    }

    private async Task ValidateBankAsync(BankTransactionWebhookRequest request, CancellationToken ct)
    {
        var bank = await bankRepository.GetBankByNameAsync(request.BankCode, ct);

        if (bank == null)
        {
            logger.LogWarning("Bank not found");
            throw new ValidationException($"The {request.BankCode} bank isn't  valid bank.");
        }

        if (!bank.Contracted)
        {
            logger.LogWarning("Bank is not contracted.");
            throw new ValidationException($"The {request.BankCode} bank isn't contracted anymore.");
        }
    }
}