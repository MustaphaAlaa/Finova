using Transaction.Contracts.IFactory;
using Transactions.Domain;

namespace Transactions.Application.Services.BankTranscationParserFactory;

public class BanqueTechParser : ITransactionStringParser
{
    public BankTransaction Parse(string transactionString)
    {
        throw new NotImplementedException();
    }
}
