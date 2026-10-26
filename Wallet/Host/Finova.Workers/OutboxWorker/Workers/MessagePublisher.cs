using System.Collections.Concurrent;
using System.Text.Json;
using Banks.Contracts.Entities;
using Banks.Contracts.Interfaces;
using MassTransit;
using Microsoft.Extensions.Options;
using Transactions.Contracts.Interfaces;

namespace OutboxWorker.Workers;

public sealed class MessagePublisher(
    IBus bus,
    IServiceScopeFactory scopeFactory,
    IOptions<WebhookWorkerOptions> options,
    ILogger<MessagePublisher> logger
) : BackgroundService
{
    private readonly WebhookWorkerOptions _opt = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var outboxWriter = scope.ServiceProvider.GetRequiredService<IBankOutboxWriter>();

                List<InboundWebhookMessage>? claimed = await outboxWriter.ClaimBatchAsync(
                    ct: stoppingToken
                );

                ConcurrentBag<InboundWebhookMessage> claimedBag = new(claimed);

                if (claimedBag.IsEmpty)
                {
                    await Task.Delay(_opt.IdleDelay, stoppingToken);
                    continue;
                }

                // Bounded parallelism. Each message gets its own connection + transaction.

                await Parallel.ForEachAsync(
                    claimedBag,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _opt.MaxConcurrency,
                        CancellationToken = stoppingToken,
                    },
                    async (message, ct) =>
                    {
                        await bus.Publish(message, ct);
                    }
                );
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "An error occurred while claiming or processing a batch."
                );
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
