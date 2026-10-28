using System.Text;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IRepositories;
using Banks.Application.Interfaces.IServices;
using Banks.Application.Interfaces.IValidations;
using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
using Banks.Contracts.Interfaces;
using Microsoft.Extensions.Logging;

namespace Banks.Application.Services;

public class BankServices(
    IBankRepository bankRepository,
    IBankOutboxWriter outboxWriter,
    IWebhookDeliveryValidator webhookDeliveryValidator,
    ILogger<BankServices> logger)
    : IBankServices
{
    public async Task WriteToOutboxAsync(BankTransactionWebhookRequest request,
        CancellationToken ct)
    {
        await webhookDeliveryValidator.ValidateNewDeliveryAsync(request, ct);


        var jsonStr = request.Payload.RootElement.GetRawText();
        long payloadSizeBytes =
            Encoding.UTF8.GetByteCount(jsonStr);


        var bank = await bankRepository.GetBankByNameAsync(request.BankCode);


        var msg = new InboundWebhookMessage
        {
            WebhookDeliveryId = request.WebhookDeliveryId,
            EventType = InboundWebhookEventType.TransactionNotification,
            BankId = bank.Id,
            BankCode = request.BankCode,
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