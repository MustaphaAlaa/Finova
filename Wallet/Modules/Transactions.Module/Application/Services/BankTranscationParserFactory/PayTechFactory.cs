using Banks.Application.Interfaces.IFactory;
using Transaction.Contracts.IFactory;
using Transactions.Application.Services.BankTranscationParserFactory;

namespace Transactions.Application.Services;

public class PayTechFactory : IBankServicesFactory
{
    public ITransactionStringParser CreateTransactionParser()
    {
        return new PayTechParser();
    }
}