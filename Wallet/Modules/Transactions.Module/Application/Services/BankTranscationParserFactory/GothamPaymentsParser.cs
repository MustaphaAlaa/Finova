using Transaction.Contracts.IFactory; 
using Transactions.Domain;

namespace Transactions.Application.Services.BankTranscationParserFactory;

public class GothamPaymentsParser : ITransactionStringParser
{
    public BankTransaction Parse(string transactionString)
    {
        throw new NotImplementedException();
    }
}
 