using Transaction.Contracts.Interfaces;
using Transactions.Application.Interfaces.IFactory;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class GothamPaymentsFactory : IBankTransactionFactory
{
    public ITransactionStringParser CreateTransactionParser()
    {
        return new GothamPaymentsParser();
    }
}
