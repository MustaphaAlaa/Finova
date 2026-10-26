using Transactions.Application.Interfaces.IFactory;
using Transactions.Contracts.Entities;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class PayTechFactory : IBankTransactionFactory
{ 
    public ITransactionStringParser CreateTransactionParser()
    {
        return new PayTechParser();
    }
}
