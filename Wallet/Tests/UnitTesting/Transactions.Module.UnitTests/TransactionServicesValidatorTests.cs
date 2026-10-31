using System.Collections.Concurrent;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Moq;
using Transaction.Contracts;
using Transaction.Contracts.Entities;
using Transaction.Contracts.Enums;
using Transactions.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Transactions.Module.UnitTests;

public class TransactionServicesValidatorTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());
    private readonly Mock<ITransactionRepository> _repository;
    private readonly TransactionServicesValidator _sut;

    public TransactionServicesValidatorTests()
    {
        _repository = _fixture.Freeze<Mock<ITransactionRepository>>();
        _sut = new TransactionServicesValidator(
            _repository.Object,
            NullLogger<TransactionServicesValidator>.Instance);
    }

    private static TransactionsRequest ValidRequest() => new()
    {
        TransactionId = Guid.NewGuid(),
        Source = Guid.NewGuid(),
        Destination = Guid.NewGuid(),
        Currency = "USD",
        Transaction = "valid transaction"
    };

    [Fact]
    public void ValidateTransactionRequest_WithValidCurrencyAndFields_DoesNotThrow()
    {
        var request = ValidRequest();
        request.Currency = "  uSd  ";

        var act = () => _sut.ValidateTransactionRequest(request);

        act.Should().NotThrow();
        // Catches valid currency codes being rejected because of case or surrounding spaces.
    }

    [Theory]
    [InlineData("transaction id", "TransactionId is required.")]
    [InlineData("source", "Source is required.")]
    [InlineData("destination", "Destination is required.")]
    [InlineData("currency", "Currency is required.")]
    [InlineData("transaction", "Transaction is required.")]
    public void ValidateTransactionRequest_WhenRequiredFieldIsMissing_ThrowsArgumentException(
        string missingField,
        string expectedMessage)
    {
        var request = ValidRequest();
        switch (missingField)
        {
            case "transaction id": request.TransactionId = Guid.Empty; break;
            case "source": request.Source = Guid.Empty; break;
            case "destination": request.Destination = Guid.Empty; break;
            case "currency": request.Currency = " "; break;
            case "transaction": request.Transaction = "\t"; break;
        }

        var act = () => _sut.ValidateTransactionRequest(request);

        act.Should().Throw<ArgumentException>().WithMessage(expectedMessage);
        // Catches incomplete requests being accepted and sent for transaction parsing.
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public void ValidateTransactionRequest_WhenCurrencyIsNullOrWhitespace_ThrowsCurrencyRequiredException(string? currency)
    {
        var request = ValidRequest();
        request.Currency = currency!;

        var act = () => _sut.ValidateTransactionRequest(request);

        act.Should().Throw<ArgumentException>().WithMessage("Currency is required.");
        // Catches null, empty, or whitespace currency values bypassing required-field validation.
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public void ValidateTransactionRequest_WhenTransactionTextIsNullOrWhitespace_ThrowsTransactionRequiredException(string? transaction)
    {
        var request = ValidRequest();
        request.Transaction = transaction!;

        var act = () => _sut.ValidateTransactionRequest(request);

        act.Should().Throw<ArgumentException>().WithMessage("Transaction is required.");
        // Catches null, empty, or whitespace transaction strings reaching a bank parser.
    }

    [Fact]
    public void ValidateTransactionRequest_WhenSourceAndDestinationAreTheSame_ThrowsArgumentException()
    {
        var request = ValidRequest();
        request.Destination = request.Source;

        var act = () => _sut.ValidateTransactionRequest(request);

        act.Should().Throw<ArgumentException>().WithMessage("Source and destination cannot be the same.");
        // Catches a transfer that would debit and credit the same account.
    }

    [Theory]
    [InlineData("XXX")]
    [InlineData("999")]
    public void ValidateTransactionRequest_WhenCurrencyIsNotSupported_ThrowsArgumentException(string currency)
    {
        var request = ValidRequest();
        request.Currency = currency;

        var act = () => _sut.ValidateTransactionRequest(request);

        act.Should().Throw<ArgumentException>().WithMessage($"Currency '{currency}' is not supported.");
        // Catches unknown currency codes entering the processing pipeline.
    }

    [Fact]
    public async Task ValidateTransactionRequestsAsync_WithValidRequests_ReturnsAllWithoutWritingRejections()
    {
        var requests = Enumerable.Range(0, 3).Select(_ => ValidRequest()).ToList();

        var result = await _sut.ValidateTransactionRequestsAsync(requests, CancellationToken.None);

        result.Should().BeEquivalentTo(requests);
        _repository.Verify(x => x.WriteTransactionsProcessingRecordAsync(
            It.IsAny<ConcurrentBag<TransacitonProcessingRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
        // Catches valid requests being dropped or incorrectly recorded as rejected.
    }

    [Fact]
    public async Task ValidateTransactionRequestsAsync_WithMixedRequests_ReturnsValidOnesAndPersistsRejectedReasons()
    {
        var valid = ValidRequest();
        var invalid = ValidRequest();
        invalid.TransactionId = Guid.Empty;
        ConcurrentBag<TransacitonProcessingRecord>? persisted = null;
        _repository.Setup(x => x.WriteTransactionsProcessingRecordAsync(
                It.IsAny<ConcurrentBag<TransacitonProcessingRecord>>(), It.IsAny<CancellationToken>()))
            .Callback<ConcurrentBag<TransacitonProcessingRecord>, CancellationToken>((records, _) => persisted = records)
            .ReturnsAsync(1);

        var result = await _sut.ValidateTransactionRequestsAsync(new() { valid, invalid }, CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeSameAs(valid);
        persisted.Should().ContainSingle();
        persisted!.Single().TransactionId.Should().Be(Guid.Empty);
        persisted.Single().Status.Should().Be(TransactionMessageProcessingStatus.Rejected);
        persisted.Single().Reason.Should().Be("TransactionId is required.");
        // Catches rejected requests being returned as valid or rejection details not being persisted.
    }

    [Fact]
    public async Task ValidateTransactionRequestsAsync_WithEmptyList_ReturnsEmptyAndDoesNotWriteRecords()
    {
        var result = await _sut.ValidateTransactionRequestsAsync([], CancellationToken.None);

        result.Should().BeEmpty();
        _repository.Verify(x => x.WriteTransactionsProcessingRecordAsync(
            It.IsAny<ConcurrentBag<TransacitonProcessingRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
        // Catches empty batches causing needless repository writes or fabricated rejections.
    }

    [Fact]
    public async Task ValidateTransactionRequestsAsync_WithNullList_ThrowsArgumentNullException()
    {
        var act = () => _sut.ValidateTransactionRequestsAsync(null!, CancellationToken.None);

        (await act.Should().ThrowAsync<ArgumentNullException>())
            .Which.ParamName.Should().Be("requests");
        // Catches null batches being silently treated as empty or failing with an unrelated exception.
    }

    [Fact]
    public async Task ValidateTransactionRequestsAsync_WhenCancelled_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => _sut.ValidateTransactionRequestsAsync(
            new() { ValidRequest() }, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        // Catches cancelled validation continuing to return a partial result.
    }
}
