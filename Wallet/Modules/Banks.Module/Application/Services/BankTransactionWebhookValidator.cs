using System.ComponentModel.DataAnnotations;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IRepositories;
using Banks.Application.Interfaces.IValidations;
using Microsoft.Extensions.Logging;

namespace Banks.Application.Services;

public class BankTransactionWebhookValidator(
    IBankRepository bankRepository,
    IBankOutboxWriter outboxWriter,
    ILogger<BankTransactionWebhookValidator> logger
) : IBankTransactionWebhookValidator
{
    public async Task ValidateAndEnsureNewDeliveryAsync(BankTransactionWebhookRequest request, CancellationToken ct)
    {
        ValidateRequest(request);

        await ValidateRequestBank(request);

        var deliveryAlreadyExists = await outboxWriter.IsWebhookDeliveryIdExistAsync(request.WebhookDeliveryId, ct);

        if (deliveryAlreadyExists)
        {
            logger.LogWarning("Transaction Webhook Request is already exist in outbox table");
            throw new ValidationException("Transaction Webhook Request is already sent to us.");
        }
    }

    void ValidateRequest(BankTransactionWebhookRequest request)
    {
        if (request.WebhookDeliveryId == Guid.Empty)
        {
            logger.LogWarning("Webhook DeliveryId is empty");
            throw new ValidationException("Webhook deliveryId is empty");
        }

        if (string.IsNullOrWhiteSpace(request.Payload))
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

    private async Task ValidateRequestBank(BankTransactionWebhookRequest request)
    {
        var bank = await bankRepository.GetBankByName(request.BankCode);

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