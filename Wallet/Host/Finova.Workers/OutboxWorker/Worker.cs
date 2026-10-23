using Banks.Contracts.Interfaces;

namespace OutboxWorker;

public class Worker(IServiceProvider serviceProvider, ILogger<Worker> logger) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            using var scope = serviceProvider.CreateScope();
            var outboxPattern = scope.ServiceProvider.GetRequiredService<IBankOutboxWriter>();
            var lst = await outboxPattern.ClaimBatchAsync();


            if (lst.Count < 1)
                logger.LogInformation("There's no messages");
            else
            {
                foreach (var item in lst)
                {
                    System.Console.WriteLine(item.Payload.RootElement.GetRawText().ToString());
                }
            }


            await Task.Delay(3000, stoppingToken);
        }
    }
}
