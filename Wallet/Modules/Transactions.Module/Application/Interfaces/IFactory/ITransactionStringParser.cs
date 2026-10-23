using Transactions.Domain;

namespace Transactions.Application.Interfaces.IFactory;

public interface ITransactionStringParser
{
    BankTransaction Parse(string transactionString);
}