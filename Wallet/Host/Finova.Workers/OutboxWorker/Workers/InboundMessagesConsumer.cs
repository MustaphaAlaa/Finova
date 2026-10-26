using Banks.Application.Interfaces.IRepositories;
using Banks.Contracts.Entities;
using MassTransit;
using Transactions.Contracts.Interfaces;

namespace OutboxWorker.Workers;

public class InboundWebhookMessagesConsumer(
    IBankRepository bankRepository,
    ITransactionServies transactionServies,
    ILogger<InboundWebhookMessagesConsumer> logger
) : IConsumer<InboundWebhookMessage>
{
    public async Task Consume(ConsumeContext<InboundWebhookMessage> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        logger.LogInformation(
            "Started processing inbound webhook message {MessageId} for bank {BankCode}.",
            message.Id,
            message.BankCode
        );

        try
        {
            await bankRepository.UpdateMessageStatusAsync(
                message,
                Banks.Contracts.Enums.InboundTransactionWebhookOutboxMessageStatus.Processing,
                cancellationToken
            );

            await transactionServies.ProcessInboundTransactionMessageAsync(
                message,
                bankRepository.UpdateMessageStatusAsync,
                cancellationToken
            );

            await bankRepository.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Successfully processed inbound webhook message {MessageId} for bank {BankCode}.",
                message.Id,
                message.BankCode
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Processing inbound webhook message {MessageId} for bank {BankCode} was cancelled.",
                message.Id,
                message.BankCode
            );

            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to process inbound webhook message {MessageId} for bank {BankCode}.",
                message.Id,
                message.BankCode
            );

            throw;
        }
    }
}
