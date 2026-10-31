using System.Threading.Channels;
using Banks.Application.Interfaces.IRepositories;
using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;
using Transaction.Contracts.Interfaces;

namespace OutboxWorker.Workers;

public sealed class InboundWebhookMessageChannel(
    IServiceScopeFactory scopeFactory,
    ILogger<InboundWebhookMessageChannel> logger
) : IHostedService
{
    private readonly Channel<WorkItem> _channel = Channel.CreateBounded<WorkItem>(
        new BoundedChannelOptions(1500)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        }
    );
    private Task[] _readerTasks = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _readerTasks =
        [
            ReadMessagesAsync(),
            ReadMessagesAsync(),
            ReadMessagesAsync(),
            ReadMessagesAsync(),
            ReadMessagesAsync(),
            ReadMessagesAsync(),
            ReadMessagesAsync(),
            ReadMessagesAsync(),
        ];
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        await Task.WhenAll(_readerTasks).WaitAsync(cancellationToken);
    }

    public async Task EnqueueAsync(
        InboundWebhookMessage message,
        CancellationToken cancellationToken
    )
    {
        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        await _channel.Writer.WriteAsync(
            new WorkItem(message, cancellationToken, completion),
            cancellationToken
        );
        await completion.Task;
    }

    private async Task ReadMessagesAsync()
    {
        await foreach (var workItem in _channel.Reader.ReadAllAsync())
        {
            await ProcessMessageAsync(workItem);
        }
    }

    private async Task ProcessMessageAsync(WorkItem workItem)
    {
        try
        {
            workItem.CancellationToken.ThrowIfCancellationRequested();

            using var scope = scopeFactory.CreateScope();

            var bankRepository = scope.ServiceProvider.GetRequiredService<IBankRepository>();

            var transactionServices =
                scope.ServiceProvider.GetRequiredService<ITransactionServies>();

            logger.LogInformation(
                "Started processing inbound webhook message {MessageId} for bank {BankCode}.",
                workItem.Message.Id,
                workItem.Message.BankCode
            );

            await bankRepository.UpdateMessageStatusAsync(
                workItem.Message,
                InboundTransactionWebhookOutboxMessageStatus.Processing,
                workItem.CancellationToken
            );

            await transactionServices.ProcessInboundTransactionMessageAsync(
                workItem.Message,
                bankRepository.UpdateMessageStatusAsync,
                workItem.CancellationToken
            );

            await bankRepository.SaveChangesAsync(workItem.CancellationToken);

            logger.LogInformation(
                "Successfully processed inbound webhook message {MessageId} for bank {BankCode}.",
                workItem.Message.Id,
                workItem.Message.BankCode
            );

            workItem.Completion.TrySetResult(true);
        }
        catch (OperationCanceledException) when (workItem.CancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Processing inbound webhook message {MessageId} for bank {BankCode} was cancelled.",
                workItem.Message.Id,
                workItem.Message.BankCode
            );
            workItem.Completion.TrySetCanceled(workItem.CancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to process inbound webhook message {MessageId} for bank {BankCode}.",
                workItem.Message.Id,
                workItem.Message.BankCode
            );
            workItem.Completion.TrySetException(exception);
        }
    }

    private sealed record WorkItem(
        InboundWebhookMessage Message,
        CancellationToken CancellationToken,
        TaskCompletionSource<bool> Completion
    );
}
