using AutoFixture;
using AutoFixture.AutoMoq;
using Banks.Application.Interfaces.IRepositories;
using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OutboxWorker.Workers;
using Transaction.Contracts.Interfaces;

namespace OutboxWorker.UnitTests;

public class InboundWebhookMessagesConsumerTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());

    [Fact]
    public async Task Consume_ForwardsMessageAndCancellationTokenAndWaitsForChannelCompletion()
    {
        var message = new InboundWebhookMessage
        {
            Id = Guid.NewGuid(),
            WebhookDeliveryId = Guid.NewGuid(),
            BankId = Guid.NewGuid(),
            BankCode = ContractedBanks.Acme,
            Payload = System.Text.Json.JsonDocument.Parse("[]"),
            PayloadSizeBytes = 2
        };
        using var cancellation = new CancellationTokenSource();
        var context = _fixture.Freeze<Mock<ConsumeContext<InboundWebhookMessage>>>();
        context.SetupGet(x => x.Message).Returns(message);
        context.SetupGet(x => x.CancellationToken).Returns(cancellation.Token);

        var bankRepository = _fixture.Freeze<Mock<IBankRepository>>();
        bankRepository.Setup(x => x.UpdateMessageStatusAsync(
                message, It.IsAny<InboundTransactionWebhookOutboxMessageStatus>(), cancellation.Token))
            .ReturnsAsync(1);
        bankRepository.Setup(x => x.SaveChangesAsync(cancellation.Token)).ReturnsAsync(1);
        var transactionServices = _fixture.Freeze<Mock<ITransactionServies>>();
        transactionServices.Setup(x => x.ProcessInboundTransactionMessageAsync(
                message,
                It.IsAny<Func<InboundWebhookMessage, InboundTransactionWebhookOutboxMessageStatus, CancellationToken, Task<int>>>(),
                cancellation.Token))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddScoped<IBankRepository>(_ => bankRepository.Object);
        services.AddScoped<ITransactionServies>(_ => transactionServices.Object);
        using var provider = services.BuildServiceProvider();
        var channel = new InboundWebhookMessageChannel(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<InboundWebhookMessageChannel>.Instance);
        var consumer = new InboundWebhookMessagesConsumer(channel);
        await channel.StartAsync(CancellationToken.None);
        try
        {
            await consumer.Consume(context.Object);

            transactionServices.Verify(x => x.ProcessInboundTransactionMessageAsync(
                message,
                It.IsAny<Func<InboundWebhookMessage, InboundTransactionWebhookOutboxMessageStatus, CancellationToken, Task<int>>>(),
                cancellation.Token), Times.Once);
            bankRepository.Verify(x => x.SaveChangesAsync(cancellation.Token), Times.Once);
            // Catches acknowledgement before queued processing finishes or loss of the broker cancellation token.
        }
        finally
        {
            await channel.StopAsync(CancellationToken.None);
        }
    }
}
