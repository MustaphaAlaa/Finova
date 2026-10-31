using System.Text;
using System.Text.Json;
using AutoFixture;
using AutoFixture.AutoMoq;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IRepositories;
using Banks.Application.Interfaces.IValidations;
using Banks.Application.Services;
using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
using Banks.Contracts.Interfaces;
using Banks.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Banks.Module.UnitTests;

public class BankServicesTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());
    private readonly Mock<IBankRepository> _bankRepository;
    private readonly Mock<IBankOutboxWriter> _outboxWriter;
    private readonly Mock<IWebhookDeliveryValidator> _validator;
    private readonly BankServices _sut;

    public BankServicesTests()
    {
        _bankRepository = _fixture.Freeze<Mock<IBankRepository>>();
        _outboxWriter = _fixture.Freeze<Mock<IBankOutboxWriter>>();
        _validator = _fixture.Freeze<Mock<IWebhookDeliveryValidator>>();
        _sut = new BankServices(
            _bankRepository.Object,
            _outboxWriter.Object,
            _validator.Object,
            NullLogger<BankServices>.Instance);
    }

    [Fact]
    public async Task WriteToOutboxAsync_WithValidRequest_PersistsPendingNotificationAndSavesChanges()
    {
        using var payload = JsonDocument.Parse("{\"message\":\"café\",\"amount\":10}");
        var request = new BankTransactionWebhookRequest(Guid.NewGuid(), ContractedBanks.Acme, payload);
        var bankId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        _bankRepository.Setup(x => x.GetBankByNameAsync(request.BankCode))
            .ReturnsAsync(new Bank { Id = bankId, Name = request.BankCode, Contracted = true });

        InboundWebhookMessage? saved = null;
        _outboxWriter.Setup(x => x.CreateAsync(It.IsAny<InboundWebhookMessage>(), cancellation.Token))
            .Callback<InboundWebhookMessage, CancellationToken>((message, _) => saved = message)
            .ReturnsAsync((InboundWebhookMessage message, CancellationToken _) => message);

        await _sut.WriteToOutboxAsync(request, cancellation.Token);

        saved.Should().NotBeNull();
        saved!.WebhookDeliveryId.Should().Be(request.WebhookDeliveryId);
        saved.EventType.Should().Be(InboundWebhookEventType.TransactionNotification);
        saved.BankId.Should().Be(bankId);
        saved.BankCode.Should().Be(request.BankCode);
        saved.Payload.Should().BeSameAs(payload);
        saved.PayloadSizeBytes.Should().Be(Encoding.UTF8.GetByteCount(payload.RootElement.GetRawText()));
        saved.Status.Should().Be(InboundTransactionWebhookOutboxMessageStatus.Pending);
        saved.ReceivedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _validator.Verify(x => x.ValidateNewDeliveryAsync(request, cancellation.Token), Times.Once);
        _outboxWriter.Verify(x => x.CreateAsync(It.IsAny<InboundWebhookMessage>(), cancellation.Token), Times.Once);
        _outboxWriter.Verify(x => x.SaveChangesAsync(cancellation.Token), Times.Once);
        // Catches incorrect message metadata, character-count sizing for UTF-8, or skipped persistence.
    }

    [Fact]
    public async Task WriteToOutboxAsync_WhenValidationFails_DoesNotReadBankOrWriteToOutbox()
    {
        var request = new BankTransactionWebhookRequest(
            Guid.NewGuid(), ContractedBanks.Acme, JsonDocument.Parse("{}"));
        var expected = new System.ComponentModel.DataAnnotations.ValidationException("invalid request");
        _validator.Setup(x => x.ValidateNewDeliveryAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expected);

        var act = () => _sut.WriteToOutboxAsync(request, CancellationToken.None);

        (await act.Should().ThrowAsync<System.ComponentModel.DataAnnotations.ValidationException>())
            .Which.Should().BeSameAs(expected);
        _bankRepository.Verify(x => x.GetBankByNameAsync(It.IsAny<ContractedBanks>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(x => x.CreateAsync(It.IsAny<InboundWebhookMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        // Catches writes occurring before validation has accepted a delivery.
    }
}
