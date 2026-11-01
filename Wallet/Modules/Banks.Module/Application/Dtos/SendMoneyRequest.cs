namespace Banks.Application.Dtos;

public sealed record SendMoneyRequest(
    Guid SenderBankAccountId,
    decimal Amount,
    string Currency,
    string ReceiverBankCode,
    string ReceiverAccountNumber,
    string BeneficiaryName,
    IReadOnlyCollection<string>? Notes = null,
    int PaymentType = 99,
    string ChargeDetails = "SHA");
