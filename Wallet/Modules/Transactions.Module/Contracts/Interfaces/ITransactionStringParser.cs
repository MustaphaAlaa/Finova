namespace Transactions.Contracts.Entities;

public interface ITransactionStringParser
{
    BankTransaction Parse(string transactionString);
}
