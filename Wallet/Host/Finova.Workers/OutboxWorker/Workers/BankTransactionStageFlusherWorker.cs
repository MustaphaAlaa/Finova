using Transaction.Contracts.Interfaces;

namespace OutboxWorker.Workers;

public sealed class BankTransactionStageFlusherWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<BankTransactionStageFlusherWorker> logger
) : BackgroundService
{
    private const int BatchSize = 32_000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var flusherWorker =
                    scope.ServiceProvider.GetRequiredService<IBankTransactionStageFlusher>();
                // Drain: keep going while full batches are available
                while (await flusherWorker.FlushOnceAsync(BatchSize, stoppingToken) == BatchSize)
                { }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Stage flush failed; will retry on next tick");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
