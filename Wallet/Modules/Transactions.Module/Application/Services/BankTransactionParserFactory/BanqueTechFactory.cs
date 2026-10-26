using Transactions.Application.Interfaces.IFactory;
using Transactions.Contracts.Entities;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class BanqueTechFactory : IBankTransactionFactory
{
    public ITransactionStringParser CreateTransactionParser()
    {
        return new BanqueTechParser();
    }
}
