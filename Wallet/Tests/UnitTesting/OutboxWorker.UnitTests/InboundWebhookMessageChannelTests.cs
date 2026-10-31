using AutoFixture;
using AutoFixture.AutoMoq;
using Banks.Application.Interfaces.IRepositories;
using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Transaction.Contracts.Interfaces;
using OutboxWorker.Workers;

namespace OutboxWorker.UnitTests;

public class InboundWebhookMessageChannelTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());

    [Fact]
    public async Task EnqueueAsync_WhenMessageProcessingSucceeds_CompletesAfterSavingRepositoryChanges()
    {
        var bankRepository = _fixture.Freeze<Mock<IBankRepository>>();
        var transactionServices = _fixture.Freeze<Mock<ITransactionServies>>();
        bankRepository.Setup(x => x.UpdateMessageStatusAsync(
                It.IsAny<InboundWebhookMessage>(), It.IsAny<InboundTransactionWebhookOutboxMessageStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        bankRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        transactionServices.Setup(x => x.ProcessInboundTransactionMessageAsync(
                It.IsAny<InboundWebhookMessage>(), It.IsAny<Func<InboundWebhookMessage, InboundTransactionWebhookOutboxMessageStatus, CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var provider = CreateProvider(bankRepository.Object, transactionServices.Object);
        var channel = CreateChannel(provider);
        var message = CreateMessage();
        await channel.StartAsync(CancellationToken.None);
        try
        {
            await channel.EnqueueAsync(message, CancellationToken.None);

            bankRepository.Verify(x => x.UpdateMessageStatusAsync(
                message, InboundTransactionWebhookOutboxMessageStatus.Processing, It.IsAny<CancellationToken>()), Times.Once);
            transactionServices.Verify(x => x.ProcessInboundTransactionMessageAsync(
                message, It.IsAny<Func<InboundWebhookMessage, InboundTransactionWebhookOutboxMessageStatus, CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()), Times.Once);
            bankRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            // Catches acknowledgements happening before status update, processing, and save complete.
        }
        finally
        {
            await channel.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task EnqueueAsync_WhenMessageProcessingFails_PropagatesFailureToCaller()
    {
        var bankRepository = _fixture.Freeze<Mock<IBankRepository>>();
        var transactionServices = _fixture.Freeze<Mock<ITransactionServies>>();
        bankRepository.Setup(x => x.UpdateMessageStatusAsync(
                It.IsAny<InboundWebhookMessage>(), It.IsAny<InboundTransactionWebhookOutboxMessageStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var expected = new InvalidOperationException("processing failed");
        transactionServices.Setup(x => x.ProcessInboundTransactionMessageAsync(
                It.IsAny<InboundWebhookMessage>(), It.IsAny<Func<InboundWebhookMessage, InboundTransactionWebhookOutboxMessageStatus, CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expected);

        using var provider = CreateProvider(bankRepository.Object, transactionServices.Object);
        var channel = CreateChannel(provider);
        await channel.StartAsync(CancellationToken.None);
        try
        {
            var act = () => channel.EnqueueAsync(CreateMessage(), CancellationToken.None);

            (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(expected);
            bankRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            // Catches failed work being acknowledged as successful or its error being swallowed.
        }
        finally
        {
            await channel.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task EnqueueAsync_WhenProcessingTokenIsCancelled_CompletesAsCancelled()
    {
        var bankRepository = _fixture.Freeze<Mock<IBankRepository>>();
        var transactionServices = _fixture.Freeze<Mock<ITransactionServies>>();
        bankRepository.Setup(x => x.UpdateMessageStatusAsync(
                It.IsAny<InboundWebhookMessage>(), It.IsAny<InboundTransactionWebhookOutboxMessageStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        using var cancellation = new CancellationTokenSource();
        transactionServices.Setup(x => x.ProcessInboundTransactionMessageAsync(
                It.IsAny<InboundWebhookMessage>(), It.IsAny<Func<InboundWebhookMessage, InboundTransactionWebhookOutboxMessageStatus, CancellationToken, Task<int>>>(), cancellation.Token))
            .Returns(() =>
            {
                cancellation.Cancel();
                return Task.FromCanceled(cancellation.Token);
            });

        using var provider = CreateProvider(bankRepository.Object, transactionServices.Object);
        var channel = CreateChannel(provider);
        await channel.StartAsync(CancellationToken.None);
        try
        {
            var act = () => channel.EnqueueAsync(CreateMessage(), cancellation.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            bankRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            // Catches cancelled processing being reported as completed or saved as successful.
        }
        finally
        {
            await channel.StopAsync(CancellationToken.None);
        }
    }

    private static ServiceProvider CreateProvider(IBankRepository bankRepository, ITransactionServies transactionServices)
    {
        var services = new ServiceCollection();
        services.AddScoped<IBankRepository>(_ => bankRepository);
        services.AddScoped<ITransactionServies>(_ => transactionServices);
        return services.BuildServiceProvider();
    }

    private static InboundWebhookMessageChannel CreateChannel(IServiceProvider provider) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<InboundWebhookMessageChannel>.Instance);

    private static InboundWebhookMessage CreateMessage() => new()
    {
        Id = Guid.NewGuid(),
        WebhookDeliveryId = Guid.NewGuid(),
        BankId = Guid.NewGuid(),
        BankCode = ContractedBanks.Acme,
        Payload = System.Text.Json.JsonDocument.Parse("[]"),
        PayloadSizeBytes = 2
    };
}
