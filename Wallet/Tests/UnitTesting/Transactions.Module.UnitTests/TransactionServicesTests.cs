using System.Collections.Concurrent;
using System.Text.Json;
using AutoFixture;
using AutoFixture.AutoMoq;
using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Transaction.Contracts;
using Transaction.Contracts.Entities;
using Transaction.Contracts.Enums;
using Transaction.Contracts.Interfaces;
using Transactions.Application.Interfaces.IValidations;
using Transactions.Application.Services;
using Transactions.Application.Services.BankTransactionParserFactory;

namespace Transactions.Module.UnitTests;

public class TransactionServicesTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());
    private readonly Mock<IBankTransactionStager> _stager;
    private readonly Mock<ITransactionRepository> _repository;
    private readonly Mock<ITransactionServicesValidator> _validator;
    private readonly TransactionServices _sut;

    public TransactionServicesTests()
    {
        _stager = _fixture.Freeze<Mock<IBankTransactionStager>>();
        _repository = _fixture.Freeze<Mock<ITransactionRepository>>();
        _validator = _fixture.Freeze<Mock<ITransactionServicesValidator>>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddKeyedSingleton<Transactions.Application.Interfaces.IFactory.IBankTransactionFactory>(
            nameof(ContractedBanks.Acme), new AcmeFactory());
        var services = serviceCollection.BuildServiceProvider();
        var resolver = new BankTransactionParserResolver(services);
        _sut = new TransactionServices(
            _stager.Object,
            _repository.Object,
            resolver,
            _validator.Object,
            NullLogger<TransactionServices>.Instance);
    }

    [Fact]
    public async Task
        ProcessInboundTransactionMessageAsync_WithValidTransactions_StagesMappedTransactionsAndMarksMessageSucceeded()
    {
        var request = ValidRequest();
        var message = CreateMessage(JsonSerializer.Serialize(new[] { request }));
        _validator.Setup(x => x.ValidateTransactionRequestsAsync(
                It.IsAny<List<TransactionsRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConcurrentBag<TransactionsRequest>(new[] { request }));
        IReadOnlyCollection<BankTransaction>? staged = null;
        _stager
            .Setup(x => x.StageAsync(It.IsAny<IReadOnlyCollection<BankTransaction>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<BankTransaction>, CancellationToken>((items, _) => staged = items)
            .ReturnsAsync(1);
        TransactionMessageProcessingStatus? processingStatus = null;
        string? processingReason = "not-set";
        _repository.Setup(x => x.WriteMessageProcessingRecordAsync(
                message, It.IsAny<TransactionMessageProcessingStatus>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<InboundWebhookMessage, TransactionMessageProcessingStatus, string?, CancellationToken>((_, status,
                reason, _) =>
            {
                processingStatus = status;
                processingReason = reason;
            })
            .Returns(Task.CompletedTask);
        _repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var updatedStatus = (InboundTransactionWebhookOutboxMessageStatus?)null;
        await _sut.ProcessInboundTransactionMessageAsync(
            message,
            (updatedMessage, status, _) =>
            {
                updatedMessage.Should().BeSameAs(message);
                updatedStatus = status;
                return Task.FromResult(1);
            });

        staged.Should().ContainSingle();
        var transaction = staged!.Single();
        transaction.TransactionId.Should().Be(request.TransactionId);
        transaction.WebhookDeliveryId.Should().Be(message.WebhookDeliveryId);
        transaction.BankId.Should().Be(message.BankId);
        transaction.Source.Should().Be(request.Source);
        transaction.Destination.Should().Be(request.Destination);
        transaction.Currency.Should().Be(Currencies.USD);
        transaction.Amount.Should().Be(10.25m);
        transaction.Reference.Should().Be("ref-1");
        transaction.Id.Should().NotBeEmpty();
        updatedStatus.Should().Be(InboundTransactionWebhookOutboxMessageStatus.Succeeded);
        processingStatus.Should().Be(TransactionMessageProcessingStatus.Processed);
        processingReason.Should().BeNull();
        _repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Catches valid items being lost, mis-mapped, or not marked processed after staging.
    }

    [Fact]
    public async Task ProcessInboundTransactionMessageAsync_WithInvalidJson_MarksMessageFailedAndDoesNotStage()
    {
        var message = CreateMessage("{}");
        TransactionMessageProcessingStatus? processingStatus = null;
        string? processingReason = null;
        _repository.Setup(x => x.WriteMessageProcessingRecordAsync(
                message, It.IsAny<TransactionMessageProcessingStatus>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<InboundWebhookMessage, TransactionMessageProcessingStatus, string?, CancellationToken>((_, status,
                reason, _) =>
            {
                processingStatus = status;
                processingReason = reason;
            })
            .Returns(Task.CompletedTask);
        _repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var updatedStatus = (InboundTransactionWebhookOutboxMessageStatus?)null;
        await _sut.ProcessInboundTransactionMessageAsync(
            message,
            (_, status, _) =>
            {
                updatedStatus = status;
                return Task.FromResult(1);
            });

        updatedStatus.Should().Be(InboundTransactionWebhookOutboxMessageStatus.Failed);
        processingStatus.Should().Be(TransactionMessageProcessingStatus.Rejected);
        processingReason.Should().StartWith("Invalid JSON payload:");
        _stager.Verify(
            x => x.StageAsync(It.IsAny<IReadOnlyCollection<BankTransaction>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _validator.Verify(
            x => x.ValidateTransactionRequestsAsync(It.IsAny<List<TransactionsRequest>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        // Catches invalid message shapes being marked successful or passed to the stager.
    }

    [Fact]
    public async Task
        ProcessInboundTransactionMessageAsync_WhenIndividualTransactionCannotBeParsed_PersistsRejectionAndStagesOtherValidItems()
    {
        var valid = ValidRequest();
        var invalid = ValidRequest();
        invalid.TransactionId = Guid.NewGuid();
        invalid.Transaction = "malformed";
        var message = CreateMessage(JsonSerializer.Serialize(new[] { valid, invalid }));
        _validator.Setup(x => x.ValidateTransactionRequestsAsync(
                It.IsAny<List<TransactionsRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConcurrentBag<TransactionsRequest>(new[] { valid, invalid }));
        IReadOnlyCollection<BankTransaction>? staged = null;
        _stager
            .Setup(x => x.StageAsync(It.IsAny<IReadOnlyCollection<BankTransaction>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<BankTransaction>, CancellationToken>((items, _) => staged = items)
            .ReturnsAsync(1);
        ConcurrentBag<TransacitonProcessingRecord>? rejectionRecords = null;
        _repository.Setup(x => x.WriteTransactionsProcessingRecordAsync(
                It.IsAny<ConcurrentBag<TransacitonProcessingRecord>>(), It.IsAny<CancellationToken>()))
            .Callback<ConcurrentBag<TransacitonProcessingRecord>, CancellationToken>((records, _) =>
                rejectionRecords = records)
            .ReturnsAsync(1);
        _repository.Setup(x => x.WriteMessageProcessingRecordAsync(
                message, It.IsAny<TransactionMessageProcessingStatus>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.ProcessInboundTransactionMessageAsync(message, (_, _, _) => Task.FromResult(1));

        staged.Should().ContainSingle().Which.TransactionId.Should().Be(valid.TransactionId);
        rejectionRecords.Should().ContainSingle();
        rejectionRecords!.Single().TransactionId.Should().Be(invalid.TransactionId);
        rejectionRecords.Single().Status.Should().Be(TransactionMessageProcessingStatus.Rejected);
        // Catches one malformed entry aborting the full batch or failing to record its rejection.
    }

    [Fact]
    public async Task
        ProcessInboundTransactionMessageAsync_WhenStatusUpdateDoesNotAffectExactlyOneRow_ThrowsInvalidOperationException()
    {
        var message = CreateMessage("{}");
        var act = () => _sut.ProcessInboundTransactionMessageAsync(
            message,
            (_, _, _) => Task.FromResult(0));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage($"Failed to update status for inbound message {message.Id}.");
        _repository.Verify(x => x.WriteMessageProcessingRecordAsync(
            It.IsAny<InboundWebhookMessage>(), It.IsAny<TransactionMessageProcessingStatus>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        // Catches a message being reported processed when its durable outbox status was not updated.
    }

    [Fact]
    public void CreateBankTransaction_WithSupportedBank_MapsParsedTransactionToRequestAndWebhook()
    {
        var message = CreateMessage("{}");

        var request = ValidRequest();

        var result = _sut.CreateBankTransaction(message, request);
        result.BankId.Should().Be(message.BankId);
        result.Source.Should().Be(request.Source);
        result.Destination.Should().Be(request.Destination);
        result.WebhookDeliveryId.Should().Be(request.WebhookDeliveryId);
        result.Amount.Should().Be(10.25m);
        result.Reference.Should().Be("ref-1");
    }

    private static TransactionsRequest ValidRequest() => new()
    {
        TransactionId = Guid.NewGuid(),
        WebhookDeliveryId = Guid.NewGuid(),
        Source = Guid.NewGuid(),
        Destination = Guid.NewGuid(),
        Currency = "USD",
        Transaction = "10.25//ref-1//202610311425"
    };

    private static InboundWebhookMessage CreateMessage(string json) => new()
    {
        Id = Guid.NewGuid(),
        WebhookDeliveryId = Guid.NewGuid(),
        BankId = Guid.NewGuid(),
        BankCode = ContractedBanks.Acme,
        Payload = JsonDocument.Parse(json),
        PayloadSizeBytes = json.Length
    };
}