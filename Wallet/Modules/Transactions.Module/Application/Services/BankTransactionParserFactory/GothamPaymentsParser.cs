using Transaction.Contracts.Entity;
using Transaction.Contracts.Interfaces;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class GothamPaymentsParser : ITransactionStringParser
{
    public BankTransaction Parse(string transactionString)
    {
        throw new NotImplementedException();
    }
}
