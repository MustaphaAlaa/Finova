using Transaction.Contracts.Entities;

namespace Transaction.Contracts.Interfaces;

public interface ITransactionStringParser
{
    BankTransaction Parse(string transactionString);
}
