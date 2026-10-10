namespace Transaction.Application;

public sealed class ParsedTransaction
{
    public DateTime Date { get; init; }
    public decimal Amount { get; init; }
    public string Reference { get; init; } = string.Empty;
    public Dictionary<string, string> Metadata { get; init; } = [];
}

public sealed class ReceiveMoneyTransaction
{
    public Guid BankAccountId { get; set; }
    public List<string> transactions { get; set; }
}

public sealed class ReceiveMoneyTransactionWebhook
{
    public Guid BankId { get; set; }
    public List<ReceiveMoneyTransaction> transactions { get; set; }
}
