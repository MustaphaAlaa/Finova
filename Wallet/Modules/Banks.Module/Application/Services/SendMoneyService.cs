using System.Globalization;
using System.Xml.Linq;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IRepositories;
using Banks.Application.Interfaces.IServices;
using Banks.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Banks.Application.Services;

public sealed class SendMoneyService(
    IBankAccountTransferRepository bankAccountRepository,
    ILogger<SendMoneyService> logger)
    : ISendMoneyService
{
    public async Task<string> CreatePaymentRequestXmlAsync(
        Guid applicationUserId,
        SendMoneyRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Payment request creation started for {OperationName}.",
            nameof(CreatePaymentRequestXmlAsync));
        ValidatePaymentRequest(applicationUserId, request);

        var senderAccount = await GetSenderAccountAsync(
            request.SenderBankAccountId,
            applicationUserId,
            cancellationToken);

        logger.LogInformation(
            "Sender account retrieved successfully for payment type {PaymentType}.",
            request.PaymentType);

        ValidateSenderAccountCurrency(senderAccount, request);
        ValidateSenderAccountBalance(senderAccount, request);
        logger.LogInformation(
            "Payment validation completed for payment type {PaymentType}.",
            request.PaymentType);

        var paymentRequest = BuildPaymentRequestDocument(senderAccount, request);
        var xml = SerializePaymentRequest(paymentRequest);

        logger.LogInformation(
            "Payment request XML generated successfully for payment type {PaymentType}.",
            request.PaymentType);

        return xml;
    }

    private void ValidatePaymentRequest(Guid applicationUserId, SendMoneyRequest request)
    {
        if (request is null)
        {
            LogCreationFailure("Request");
            throw new ArgumentNullException(nameof(request));
        }

        if (applicationUserId == Guid.Empty)
        {
            LogCreationFailure("ApplicationUser");
            throw new ArgumentException("An authenticated wallet user is required.", nameof(applicationUserId));
        }

        if (request.Amount <= 0)
        {
            LogCreationFailure("Amount");
            throw new ArgumentOutOfRangeException(nameof(request), "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Currency) ||
            string.IsNullOrWhiteSpace(request.ReceiverBankCode) ||
            string.IsNullOrWhiteSpace(request.ReceiverAccountNumber) ||
            string.IsNullOrWhiteSpace(request.BeneficiaryName))
        {
            LogCreationFailure("ReceiverDetails");
            throw new ArgumentException("Currency and receiver details are required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ChargeDetails))
        {
            LogCreationFailure("ChargeDetails");
            throw new ArgumentException("Charge details are required.", nameof(request));
        }
    }

    private async Task<BankAccount> GetSenderAccountAsync(
        Guid senderBankAccountId,
        Guid applicationUserId,
        CancellationToken cancellationToken)
    {
        var senderAccount = await bankAccountRepository.GetOwnedBankAccountForTransferAsync(
            senderBankAccountId,
            applicationUserId,
            cancellationToken);

        if (senderAccount is null)
        {
            LogCreationFailure("SenderAccountLookup");
            throw new KeyNotFoundException("The sender account was not found for this wallet user.");
        }

        return senderAccount;
    }

    private void ValidateSenderAccountCurrency(BankAccount senderAccount, SendMoneyRequest request)
    {
        if (!string.Equals(
                senderAccount.Currency.Code.ToString(),
                request.Currency,
                StringComparison.OrdinalIgnoreCase))
        {
            LogCreationFailure("SenderAccountCurrency");
            throw new ArgumentException(
                "The requested currency does not match the sender account currency.",
                nameof(request));
        }
    }

    private void ValidateSenderAccountBalance(BankAccount senderAccount, SendMoneyRequest request)
    {
        var balance = senderAccount.Balances
            .OrderByDescending(x => x.LastUpdate)
            .FirstOrDefault();

        if (balance is null || balance.Balance < request.Amount)
        {
            LogCreationFailure("SenderAccountBalance");
            throw new InvalidOperationException("The sender account has insufficient available balance.");
        }
    }

    private static XDocument BuildPaymentRequestDocument(
        BankAccount senderAccount,
        SendMoneyRequest request)
    {
        var root = new XElement("PaymentRequestMessage",
            BuildTransferInfo(request),
            BuildSenderInfo(senderAccount),
            BuildReceiverInfo(request));

        AddOptionalPaymentElements(root, request);

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
    }

    private static XElement BuildTransferInfo(SendMoneyRequest request)
    {
        var now = DateTimeOffset.Now;
        var dateText = now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var offsetText = now.ToString("zzz", CultureInfo.InvariantCulture);
        if (offsetText.EndsWith(":00", StringComparison.Ordinal))
            offsetText = offsetText[..^3];

        return new XElement("TransferInfo",
            new XElement("Reference", Guid.NewGuid().ToString()),
            new XElement("Date", dateText + offsetText),
            new XElement("Amount", request.Amount.ToString(
                "0.00############################",
                CultureInfo.InvariantCulture)),
            new XElement("Currency", request.Currency.ToUpperInvariant()));
    }

    private static XElement BuildSenderInfo(BankAccount senderAccount)
    {
        return new XElement("SenderInfo",
            new XElement("AccountNumber", senderAccount.AccountNumber));
    }

    private static XElement BuildReceiverInfo(SendMoneyRequest request)
    {
        return new XElement("ReceiverInfo",
            new XElement("BankCode", request.ReceiverBankCode.Trim()),
            new XElement("AccountNumber", request.ReceiverAccountNumber.Trim()),
            new XElement("BeneficiaryName", request.BeneficiaryName.Trim()));
    }

    private static void AddOptionalPaymentElements(XElement root, SendMoneyRequest request)
    {
        if (request.Notes is { Count: > 0 })
            root.Add(new XElement("Notes", request.Notes.Select(note => new XElement("Note", note))));
       
        if (request.PaymentType != 99)
            root.Add(new XElement("PaymentType", request.PaymentType.ToString(CultureInfo.InvariantCulture)));
       
        if (!string.Equals(request.ChargeDetails, "SHA", StringComparison.OrdinalIgnoreCase))
            root.Add(new XElement("ChargeDetails", request.ChargeDetails.Trim()));
    }

    private static string SerializePaymentRequest(XDocument paymentRequest)
    {
        return paymentRequest.ToString(SaveOptions.DisableFormatting);
    }

    private void LogCreationFailure(string failureStage)
    {
        logger.LogWarning(
            "Payment request creation failed at {FailureStage}.",
            failureStage);
    }
}
