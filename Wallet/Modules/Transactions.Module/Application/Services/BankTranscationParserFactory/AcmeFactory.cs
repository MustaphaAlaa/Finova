using Banks.Application.Interfaces.IFactory; 
using Transaction.Contracts.IFactory; 

namespace Transactions.Application.Services.BankTranscationParserFactory;

public class AcmeFactory : IBankServicesFactory
{
    public ITransactionStringParser CreateTransactionParser()
    {
        return new AcmeParser();
    }
}