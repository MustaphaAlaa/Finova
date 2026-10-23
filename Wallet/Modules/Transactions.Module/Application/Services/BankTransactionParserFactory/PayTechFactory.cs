  
using Transactions.Application.Interfaces.IFactory; 

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class PayTechFactory : IBankServicesFactory
{
    public ITransactionStringParser CreateTransactionParser()
    {
        return new PayTechParser();
    }
}