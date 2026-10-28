using Transaction.Contracts.Interfaces;
using Transactions.Application.Interfaces.IFactory;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class PayTechFactory : IBankTransactionFactory
{ 
    public ITransactionStringParser CreateTransactionParser()
    {
        return new PayTechParser();
    }
}
