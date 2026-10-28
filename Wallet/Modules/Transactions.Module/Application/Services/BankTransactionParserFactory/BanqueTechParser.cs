using Transaction.Contracts.Entities;
using Transaction.Contracts.Interfaces;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class BanqueTechParser : ITransactionStringParser
{
    public BankTransaction Parse(string transactionString)
    {
        throw new NotImplementedException();
    }
}
