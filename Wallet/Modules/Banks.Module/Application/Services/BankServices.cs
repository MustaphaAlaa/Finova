using System.Text;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IRepositories;
using Banks.Application.Interfaces.IServices;
using Banks.Application.Interfaces.IValidations;
using Banks.Domain;
using Banks.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Banks.Application.Services;

public class BankServices(
    IBankRepository bankRepository,
    IBankOutboxWriter outboxWriter,
    IBankTransactionWebhookValidator bankTransactionWebhookValidator,
    ILogger<BankServices> logger)
    : IBankServices
{
    public async Task WriteToOutboxAsync(BankTransactionWebhookRequest request,
        CancellationToken ct)
    {
        await bankTransactionWebhookValidator.ValidateAndEnsureNewDeliveryAsync(request, ct);

        long payloadSizeBytes =
            Encoding.UTF8.GetByteCount(request.Payload);

        var bank = await bankRepository.GetBankByName(request.BankCode);


        var msg = new InboundWebhookMessage
        {
            WebhookDeliveryId = request.WebhookDeliveryId,
            EventType = InboundWebhookEventType.TransactionNotification,
            BankId = bank.Id,
            Bank = bank,
            Payload = request.Payload,
            PayloadSizeBytes = payloadSizeBytes,
            ReceivedAtUtc = DateTime.UtcNow,
            Status = InboundTransactionWebhookOutboxMessageStatus.Pending,
        };

        await outboxWriter.CreateAsync(msg, ct);
        await outboxWriter.SaveChangesAsync(ct);

        logger.LogInformation(
            "Outbox message for webhook delivery {WebhookDeliveryId} " +
            "was created successfully.",
            request.WebhookDeliveryId);
    }
}