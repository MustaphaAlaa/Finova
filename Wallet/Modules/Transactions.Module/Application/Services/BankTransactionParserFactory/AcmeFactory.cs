using Transactions.Application.Interfaces.IFactory;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class AcmeFactory : IBankServicesFactory
{
    public ITransactionStringParser CreateTransactionParser()
    {
        return new AcmeParser();
    }
}