using Banks.Contracts.Entities;
using MassTransit;
using Transactions.Contracts.Interfaces;

namespace OutboxWorker.Workers;

public class InboundWebhookMessagesConsumer(
    ITransactionServies transactionServies,
    ILogger<InboundWebhookMessagesConsumer> logger
) : IConsumer<InboundWebhookMessage>
{
    public async Task Consume(ConsumeContext<InboundWebhookMessage> context)
    {
        await transactionServies.ProcessInboundTransactionMessageAsync(context.Message);

    }
}
