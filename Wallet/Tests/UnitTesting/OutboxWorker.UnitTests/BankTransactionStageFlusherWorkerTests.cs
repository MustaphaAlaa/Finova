using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OutboxWorker.Workers;
using Transaction.Contracts.Interfaces;

namespace OutboxWorker.UnitTests;

public class BankTransactionStageFlusherWorkerTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());

    [Fact]
    public async Task Worker_WhenFullBatchIsAvailable_DrainsAgainUntilBatchIsPartial()
    {
        var flusher = _fixture.Freeze<Mock<IBankTransactionStageFlusher>>();
        var secondFlush = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        flusher.Setup(x => x.FlushOnceAsync(32_000, It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, _) =>
            {
                if (Interlocked.Increment(ref calls) == 2)
                    secondFlush.TrySetResult();
                return Task.FromResult(calls == 1 ? 32_000L : 7L);
            });

        var services = new ServiceCollection();
        services.AddScoped<IBankTransactionStageFlusher>(_ => flusher.Object);
        using var provider = services.BuildServiceProvider();
        var worker = new BankTransactionStageFlusherWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BankTransactionStageFlusherWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            await secondFlush.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await worker.StopAsync(CancellationToken.None);

            calls.Should().Be(2);
            flusher.Verify(x => x.FlushOnceAsync(32_000, It.IsAny<CancellationToken>()), Times.Exactly(2));
            // Catches full batches being left behind instead of drained immediately.
        }
        finally
        {
            if (worker.ExecuteTask is { IsCompleted: false })
                await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Worker_WhenFlushFails_ContinuesRunningUntilStopped()
    {
        var flusher = _fixture.Freeze<Mock<IBankTransactionStageFlusher>>();
        var attemptedFlush = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        flusher.Setup(x => x.FlushOnceAsync(32_000, It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, _) =>
            {
                attemptedFlush.TrySetResult();
                return Task.FromException<long>(new InvalidOperationException("database unavailable"));
            });

        var services = new ServiceCollection();
        services.AddScoped<IBankTransactionStageFlusher>(_ => flusher.Object);
        using var provider = services.BuildServiceProvider();
        var worker = new BankTransactionStageFlusherWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BankTransactionStageFlusherWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            await attemptedFlush.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await worker.StopAsync(CancellationToken.None);

            worker.ExecuteTask.Should().NotBeNull();
            worker.ExecuteTask!.IsFaulted.Should().BeFalse();
            // Catches a transient database error terminating the hosted flusher.
        }
        finally
        {
            if (worker.ExecuteTask is { IsCompleted: false })
                await worker.StopAsync(CancellationToken.None);
        }
    }
}
