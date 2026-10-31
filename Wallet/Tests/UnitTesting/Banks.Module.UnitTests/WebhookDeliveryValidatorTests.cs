using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AutoFixture;
using AutoFixture.AutoMoq;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IRepositories;
using Banks.Application.Services.WebhookFilter;
using Banks.Contracts.Enums;
using Banks.Contracts.Interfaces;
using Banks.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Banks.Module.UnitTests;

public class WebhookDeliveryValidatorTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());
    private readonly Mock<IBankRepository> _bankRepository;
    private readonly Mock<IBankOutboxWriter> _outboxWriter;
    private readonly WebhookDeliveryValidator _sut;

    public WebhookDeliveryValidatorTests()
    {
        _bankRepository = _fixture.Freeze<Mock<IBankRepository>>();
        _outboxWriter = _fixture.Freeze<Mock<IBankOutboxWriter>>();
        _sut = new WebhookDeliveryValidator(
            _bankRepository.Object,
            _outboxWriter.Object,
            NullLogger<WebhookDeliveryValidator>.Instance);
    }

    private static BankTransactionWebhookRequest CreateRequest(
        Guid? deliveryId = null,
        ContractedBanks bankCode = ContractedBanks.Acme,
        JsonDocument? payload = null)
        => new(deliveryId ?? Guid.NewGuid(), bankCode,
            payload ?? JsonDocument.Parse("{\"amount\":10}"));

    [Fact]
    public async Task ValidateNewDeliveryAsync_WithValidNewDelivery_CompletesSuccessfully()
    {
        var request = CreateRequest();
        var cancellationToken = new CancellationTokenSource().Token;
        _bankRepository.Setup(x => x.GetBankByNameAsync(request.BankCode, cancellationToken))
            .ReturnsAsync(new Bank { Id = Guid.NewGuid(), Name = request.BankCode, Contracted = true });
        _outboxWriter.Setup(x => x.ExistsByDeliveryIdAsync(request.WebhookDeliveryId, cancellationToken))
            .ReturnsAsync(false);

        var act = () => _sut.ValidateNewDeliveryAsync(request, cancellationToken);

        await act.Should().NotThrowAsync();
        _outboxWriter.Verify(x => x.ExistsByDeliveryIdAsync(request.WebhookDeliveryId, cancellationToken), Times.Once);
        // Catches valid requests being rejected or not checked for an existing delivery.
    }

    [Fact]
    public async Task ValidateNewDeliveryAsync_WithEmptyDeliveryId_ThrowsValidationExceptionBeforeLookups()
    {
        var request = CreateRequest(deliveryId: Guid.Empty);

        var act = () => _sut.ValidateNewDeliveryAsync(request, CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).WithMessage("Webhook deliveryId is empty");
        _bankRepository.Verify(x => x.GetBankByNameAsync(It.IsAny<ContractedBanks>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(x => x.ExistsByDeliveryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        // Catches empty delivery identifiers reaching persistence or duplicate checks.
    }

    [Fact]
    public async Task ValidateNewDeliveryAsync_WithNullPayload_ThrowsValidationExceptionBeforeLookups()
    {
        var request = new BankTransactionWebhookRequest(Guid.NewGuid(), ContractedBanks.Acme, null!);

        var act = () => _sut.ValidateNewDeliveryAsync(request, CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).WithMessage("Payload is empty");
        _bankRepository.Verify(x => x.GetBankByNameAsync(It.IsAny<ContractedBanks>(), It.IsAny<CancellationToken>()), Times.Never);
        // Catches null payload dereferences and accepting missing webhook content.
    }

    [Fact]
    public async Task ValidateNewDeliveryAsync_WithEmptyOrWhitespacePayload_ThrowsValidationException()
    {
        
        var payload = JsonDocument.Parse("{}");
        var request = CreateRequest(payload: payload);

        var act = () => _sut.ValidateNewDeliveryAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        
        _bankRepository.Verify(x => x.GetBankByNameAsync(It.IsAny<ContractedBanks>(), It.IsAny<CancellationToken>()), Times.Never);
     }

    [Fact]
    public async Task ValidateNewDeliveryAsync_WithUndefinedBankCode_ThrowsValidationExceptionBeforeLookups()
    {
        var request = CreateRequest(bankCode: (ContractedBanks)int.MaxValue);

        var act = () => _sut.ValidateNewDeliveryAsync(request, CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).WithMessage("Bank Code is invalid");
        _bankRepository.Verify(x => x.GetBankByNameAsync(It.IsAny<ContractedBanks>(), It.IsAny<CancellationToken>()), Times.Never);
        // Catches enum values outside the supported bank list being sent to repository lookup.
    }

    [Fact]
    public async Task ValidateNewDeliveryAsync_WhenBankDoesNotExist_ThrowsValidationExceptionWithoutCheckingDuplicate()
    {
        var request = CreateRequest();
        _bankRepository.Setup(x => x.GetBankByNameAsync(request.BankCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bank)null!);

        var act = () => _sut.ValidateNewDeliveryAsync(request, CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>())
            .WithMessage($"The {request.BankCode} bank isn't  valid bank.");
        _outboxWriter.Verify(x => x.ExistsByDeliveryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        // Catches unknown banks being treated as eligible senders.
    }

    [Fact]
    public async Task ValidateNewDeliveryAsync_WhenBankIsNotContracted_ThrowsValidationExceptionWithoutCheckingDuplicate()
    {
        var request = CreateRequest();
        _bankRepository.Setup(x => x.GetBankByNameAsync(request.BankCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Bank { Id = Guid.NewGuid(), Name = request.BankCode, Contracted = false });

        var act = () => _sut.ValidateNewDeliveryAsync(request, CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>())
            .WithMessage($"The {request.BankCode} bank isn't contracted anymore.");
        _outboxWriter.Verify(x => x.ExistsByDeliveryIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        // Catches an inactive bank being allowed to submit webhooks.
    }

    [Fact]
    public async Task ValidateNewDeliveryAsync_WhenDeliveryIdAlreadyExists_ThrowsValidationException()
    {
        var request = CreateRequest();
        _bankRepository.Setup(x => x.GetBankByNameAsync(request.BankCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Bank { Id = Guid.NewGuid(), Name = request.BankCode, Contracted = true });
        _outboxWriter.Setup(x => x.ExistsByDeliveryIdAsync(request.WebhookDeliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.ValidateNewDeliveryAsync(request, CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>())
            .WithMessage("Transaction Webhook Request is already sent to us.");
        // Catches duplicate webhook deliveries being accepted for processing.
    }
}
