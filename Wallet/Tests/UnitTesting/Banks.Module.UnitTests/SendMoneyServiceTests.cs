using Banks.Application.Dtos;
using Banks.Application.Interfaces.IRepositories;
using Banks.Application.Services;
using Banks.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Transaction.Contracts.Entities;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Banks.Module.UnitTests;

public class SendMoneyServiceTests
{
    private static readonly Guid ApplicationUserId =
        Guid.Parse("f108f490-6007-49bf-8f5c-c2fd24d9d909");

    private readonly Mock<IBankAccountTransferRepository> _bankAccountRepository = new();
    private readonly SendMoneyService _sut;

    public SendMoneyServiceTests()
    {
        _sut = new SendMoneyService(
            _bankAccountRepository.Object,
            NullLogger<SendMoneyService>.Instance);
    }

    [Fact]
    public async Task CreatePaymentRequestXmlAsync_WithValidRequest_BuildsExpectedPaymentXml()
    {
        var senderAccount = CreateSenderAccount();
        var request = CreateRequest(
            amount: 125.5m,
            currency: "egp",
            receiverBankCode: "  BANK-02 ",
            receiverAccountNumber: "  9876543210 ",
            beneficiaryName: "  Test Beneficiary ",
            notes: ["Invoice 123", "Monthly payment"],
            paymentType: 1,
            chargeDetails: " OUR ");
        using var cancellation = new CancellationTokenSource();
        SetupSenderAccount(senderAccount, cancellation.Token);

        var xml = await _sut.CreatePaymentRequestXmlAsync(
            ApplicationUserId,
            request,
            cancellation.Token);

        var document = XDocument.Parse(xml);
        var root = document.Root!;
        root.Name.LocalName.Should().Be("PaymentRequestMessage");
        root.Elements().Select(element => element.Name.LocalName).Should().Equal(
            "TransferInfo",
            "SenderInfo",
            "ReceiverInfo",
            "Notes",
            "PaymentType",
            "ChargeDetails");

        var transferInfo = root.Element("TransferInfo")!;
        Guid.TryParse(transferInfo.Element("Reference")!.Value, out _).Should().BeTrue();
        Regex.IsMatch(
            transferInfo.Element("Date")!.Value,
            @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}[+-]\d{2}(:\d{2})?$").Should().BeTrue();
        transferInfo.Element("Amount")!.Value.Should().Be("125.50");
        transferInfo.Element("Currency")!.Value.Should().Be("EGP");

        root.Element("SenderInfo")!.Element("AccountNumber")!.Value
            .Should().Be(senderAccount.AccountNumber);
        var receiverInfo = root.Element("ReceiverInfo")!;
        receiverInfo.Element("BankCode")!.Value.Should().Be("BANK-02");
        receiverInfo.Element("AccountNumber")!.Value.Should().Be("9876543210");
        receiverInfo.Element("BeneficiaryName")!.Value.Should().Be("Test Beneficiary");
        root.Element("Notes")!.Elements("Note").Select(note => note.Value)
            .Should().Equal("Invoice 123", "Monthly payment");
        root.Element("PaymentType")!.Value.Should().Be("1");
        root.Element("ChargeDetails")!.Value.Should().Be("OUR");

        _bankAccountRepository.Verify(
            repository => repository.GetOwnedBankAccountForTransferAsync(
                request.SenderBankAccountId,
                ApplicationUserId,
                cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task CreatePaymentRequestXmlAsync_WithDefaultOptionalValues_OmitsOptionalElements()
    {
        var senderAccount = CreateSenderAccount();
        var request = CreateRequest(notes: []);
        SetupSenderAccount(senderAccount);

        var xml = await _sut.CreatePaymentRequestXmlAsync(
            ApplicationUserId,
            request,
            CancellationToken.None);

        var root = XDocument.Parse(xml).Root!;
        root.Element("Notes").Should().BeNull();
        root.Element("PaymentType").Should().BeNull();
        root.Element("ChargeDetails").Should().BeNull();
        root.Elements().Select(element => element.Name.LocalName).Should().Equal(
            "TransferInfo",
            "SenderInfo",
            "ReceiverInfo");
    }

    [Fact]
    public async Task CreatePaymentRequestXmlAsync_WithNullRequest_ThrowsBeforeRepositoryLookup()
    {
        var act = () => _sut.CreatePaymentRequestXmlAsync(
            ApplicationUserId,
            null!,
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
        VerifySenderAccountWasNotLookedUp();
    }

    [Fact]
    public async Task CreatePaymentRequestXmlAsync_WithEmptyUserId_ThrowsBeforeRepositoryLookup()
    {
        var act = () => _sut.CreatePaymentRequestXmlAsync(
            Guid.Empty,
            CreateRequest(),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("applicationUserId");
        VerifySenderAccountWasNotLookedUp();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreatePaymentRequestXmlAsync_WithNonPositiveAmount_ThrowsBeforeRepositoryLookup(
        decimal amount)
    {
        var act = () => _sut.CreatePaymentRequestXmlAsync(
            ApplicationUserId,
            CreateRequest(amount: amount),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("request");
        VerifySenderAccountWasNotLookedUp();
    }

    [Fact]
    public async Task CreatePaymentRequestXmlAsync_WithMissingRequiredDetails_ThrowsBeforeRepositoryLookup()
    {
        var act = () => _sut.CreatePaymentRequestXmlAsync(
            ApplicationUserId,
            CreateRequest(chargeDetails: " "),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("request")
            .WithMessage("Charge details are required.*");
        VerifySenderAccountWasNotLookedUp();
    }

    [Fact]
    public async Task CreatePaymentRequestXmlAsync_WhenSenderAccountIsNotFound_ThrowsKeyNotFoundException()
    {
        var request = CreateRequest();
        _bankAccountRepository
            .Setup(repository => repository.GetOwnedBankAccountForTransferAsync(
                request.SenderBankAccountId,
                ApplicationUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankAccount?)null);

        var act = () => _sut.CreatePaymentRequestXmlAsync(
            ApplicationUserId,
            request,
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("The sender account was not found for this wallet user.");
    }

    [Fact]
    public async Task CreatePaymentRequestXmlAsync_WhenCurrencyDoesNotMatch_ThrowsArgumentException()
    {
        var senderAccount = CreateSenderAccount();
        var request = CreateRequest(currency: "USD");
        SetupSenderAccount(senderAccount);

        var act = () => _sut.CreatePaymentRequestXmlAsync(
            ApplicationUserId,
            request,
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("request")
            .WithMessage("The requested currency does not match the sender account currency.*");
    }

    [Fact]
    public async Task CreatePaymentRequestXmlAsync_WhenBalanceIsInsufficient_ThrowsInvalidOperationException()
    {
        var senderAccount = CreateSenderAccount(balance: 100m);
        var request = CreateRequest(amount: 100.01m);
        SetupSenderAccount(senderAccount);

        var act = () => _sut.CreatePaymentRequestXmlAsync(
            ApplicationUserId,
            request,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The sender account has insufficient available balance.");
    }

    [Fact]
    public async Task CreatePaymentRequestXmlAsync_WhenNoBalanceExists_ThrowsInvalidOperationException()
    {
        var senderAccount = CreateSenderAccount(balance: null);
        SetupSenderAccount(senderAccount);

        var act = () => _sut.CreatePaymentRequestXmlAsync(
            ApplicationUserId,
            CreateRequest(),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The sender account has insufficient available balance.");
    }

    [Fact]
    public async Task CreatePaymentRequestXmlAsync_UsesMostRecentlyUpdatedBalance()
    {
        var senderAccount = CreateSenderAccount(balance: null);
        senderAccount.Balances =
        [
            new BankAccountBalance
            {
                BankAccountId = senderAccount.Id,
                Balance = 0m,
                LastUpdate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new BankAccountBalance
            {
                BankAccountId = senderAccount.Id,
                Balance = 500m,
                LastUpdate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            },
        ];
        SetupSenderAccount(senderAccount);

        var xml = await _sut.CreatePaymentRequestXmlAsync(
            ApplicationUserId,
            CreateRequest(amount: 450m),
            CancellationToken.None);

        XDocument.Parse(xml).Root.Should().NotBeNull();
    }

    private void SetupSenderAccount(
        BankAccount senderAccount,
        CancellationToken cancellationToken = default)
    {
        _bankAccountRepository
            .Setup(repository => repository.GetOwnedBankAccountForTransferAsync(
                senderAccount.Id,
                ApplicationUserId,
                cancellationToken))
            .ReturnsAsync(senderAccount);
    }

    private void VerifySenderAccountWasNotLookedUp()
    {
        _bankAccountRepository.Verify(
            repository => repository.GetOwnedBankAccountForTransferAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static SendMoneyRequest CreateRequest(
        decimal amount = 100m,
        string currency = "EGP",
        string receiverBankCode = "BANK-02",
        string receiverAccountNumber = "9876543210",
        string beneficiaryName = "Test Beneficiary",
        IReadOnlyCollection<string>? notes = null,
        int paymentType = 99,
        string chargeDetails = "SHA") =>
        new(
            SenderBankAccountId: Guid.Parse("a68e3e38-0de1-4ad0-92c7-21b783702f81"),
            Amount: amount,
            Currency: currency,
            ReceiverBankCode: receiverBankCode,
            ReceiverAccountNumber: receiverAccountNumber,
            BeneficiaryName: beneficiaryName,
            Notes: notes,
            PaymentType: paymentType,
            ChargeDetails: chargeDetails);

    private static BankAccount CreateSenderAccount(decimal? balance = 10_000m)
    {
        var account = new BankAccount
        {
            Id = Guid.Parse("a68e3e38-0de1-4ad0-92c7-21b783702f81"),
            AccountNumber = "TEST-ACME-0001",
            ApplicationUserId = ApplicationUserId,
            BankId = Guid.Parse("c9337615-fb2b-4be1-9da0-c9704d546bf6"),
            CurrencyId = Guid.Parse("00596123-1c69-4603-ac90-4bbe433621e9"),
            Currency = new Currency
            {
                Code = Currencies.EGP,
                Name = "Egyptian Pound",
            },
        };

        if (balance.HasValue)
        {
            account.Balances =
            [
                new BankAccountBalance
                {
                    BankAccountId = account.Id,
                    Balance = balance.Value,
                    LastUpdate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ];
        }

        return account;
    }
}
