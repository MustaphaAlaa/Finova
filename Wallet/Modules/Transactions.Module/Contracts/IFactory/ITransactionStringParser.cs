using Transactions.Domain;

namespace Transaction.Contracts.IFactory;

public interface ITransactionStringParser
{
    BankTransaction Parse(string transactionString);
}