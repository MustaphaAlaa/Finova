 
using Transactions.Application.Interfaces.IFactory;
using Transactions.Domain;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class GothamPaymentsParser : ITransactionStringParser
{
    public BankTransaction Parse(string transactionString)
    {
        throw new NotImplementedException();
    }
}
