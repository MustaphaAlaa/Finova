 using Transactions.Application.Interfaces.IFactory;
 using Transactions.Domain;
 
namespace Transactions.Application.Services.BankTransactionParserFactory;

public class BanqueTechParser : ITransactionStringParser
{
    public BankTransaction Parse(string transactionString)
    {
        throw new NotImplementedException();
    }
}
