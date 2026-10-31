using Banks.Contracts.Entities;
using MassTransit;

namespace OutboxWorker.Workers;

public class InboundWebhookMessagesConsumer(InboundWebhookMessageChannel messageChannel)
    : IConsumer<InboundWebhookMessage>
{
    public Task Consume(ConsumeContext<InboundWebhookMessage> context) =>
        messageChannel.EnqueueAsync(context.Message, context.CancellationToken);
}
