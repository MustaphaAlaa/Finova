using AutoFixture;
using AutoFixture.AutoMoq;
using Banks.Contracts.Entities;
using Banks.Contracts.Interfaces;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OutboxWorker;
using OutboxWorker.Workers;

namespace OutboxWorker.UnitTests;

public class MessagePublisherTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());

    [Fact]
    public async Task Publisher_WhenBatchContainsMessages_PublishesEachClaimedMessage()
    {
        var messages = Enumerable.Range(0, 3).Select(_ => CreateMessage()).ToList();
        var outboxWriter = _fixture.Freeze<Mock<IBankOutboxWriter>>();
        outboxWriter.Setup(x => x.ClaimBatchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);
        var bus = _fixture.Freeze<Mock<IBus>>();
        var published = new System.Collections.Concurrent.ConcurrentBag<Guid>();
        var allPublished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bus.Setup(x => x.Publish(It.IsAny<InboundWebhookMessage>(), It.IsAny<CancellationToken>()))
            .Callback<InboundWebhookMessage, CancellationToken>((message, _) =>
            {
                published.Add(message.Id);
                if (published.Count == messages.Count)
                    allPublished.TrySetResult();
            })
            .Returns(Task.CompletedTask);

        using var provider = CreateProvider(outboxWriter.Object);
        var publisher = new MessagePublisher(
            bus.Object,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new WebhookWorkerOptions { IdleDelay = TimeSpan.FromMilliseconds(10) }),
            NullLogger<MessagePublisher>.Instance);
        await publisher.StartAsync(CancellationToken.None);
        try
        {
            await allPublished.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await publisher.StopAsync(CancellationToken.None);

            published.Should().BeEquivalentTo(messages.Select(x => x.Id));
            outboxWriter.Verify(x => x.ClaimBatchAsync(1500, 10 * 1024 * 1024, It.IsAny<CancellationToken>()), Times.Once);
            // Catches claimed messages being skipped or a batch not being published in full.
        }
        finally
        {
            if (publisher.ExecuteTask is { IsCompleted: false })
                await publisher.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Publisher_WhenClaimedBatchIsEmpty_WaitsAndClaimsAgain()
    {
        var outboxWriter = _fixture.Freeze<Mock<IBankOutboxWriter>>();
        var secondClaim = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var claims = 0;
        outboxWriter.Setup(x => x.ClaimBatchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (Interlocked.Increment(ref claims) == 2)
                    secondClaim.TrySetResult();
                return Task.FromResult(new List<InboundWebhookMessage>());
            });

        using var provider = CreateProvider(outboxWriter.Object);
        var publisher = new MessagePublisher(
            _fixture.Freeze<Mock<IBus>>().Object,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new WebhookWorkerOptions { IdleDelay = TimeSpan.FromMilliseconds(10) }),
            NullLogger<MessagePublisher>.Instance);
        await publisher.StartAsync(CancellationToken.None);
        try
        {
            await secondClaim.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await publisher.StopAsync(CancellationToken.None);

            claims.Should().BeGreaterThanOrEqualTo(2);
            // Catches an empty queue causing the publisher to stop polling for new work.
        }
        finally
        {
            if (publisher.ExecuteTask is { IsCompleted: false })
                await publisher.StopAsync(CancellationToken.None);
        }
    }

    private static ServiceProvider CreateProvider(IBankOutboxWriter outboxWriter)
    {
        var services = new ServiceCollection();
        services.AddScoped<IBankOutboxWriter>(_ => outboxWriter);
        return services.BuildServiceProvider();
    }

    private static InboundWebhookMessage CreateMessage() => new()
    {
        Id = Guid.NewGuid(),
        WebhookDeliveryId = Guid.NewGuid(),
        BankId = Guid.NewGuid(),
        Payload = System.Text.Json.JsonDocument.Parse("[]"),
        PayloadSizeBytes = 2
    };
}
