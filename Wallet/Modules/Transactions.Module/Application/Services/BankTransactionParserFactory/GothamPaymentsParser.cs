using Transactions.Contracts.Entities;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class GothamPaymentsParser : ITransactionStringParser
{
    public BankTransaction Parse(string transactionString)
    {
        throw new NotImplementedException();
    }
}
