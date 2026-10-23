using Transactions.Application.Interfaces.IFactory;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class BanqueTechFactory : IBankServicesFactory
{
    public ITransactionStringParser CreateTransactionParser()
    {
        return new BanqueTechParser();
    }
}