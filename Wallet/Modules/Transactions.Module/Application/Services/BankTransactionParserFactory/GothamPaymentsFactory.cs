using Transactions.Application.Interfaces.IFactory;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class GothamPaymentsFactory : IBankServicesFactory
{
    public ITransactionStringParser CreateTransactionParser()
    {
        return new GothamPaymentsParser();
    }
}