using Transactions.Application.Interfaces.IFactory;
using Transactions.Contracts.Entities;

namespace Transactions.Application.Services.BankTransactionParserFactory;

// Suggest better names instead of IBankServicesFactory, BankServicesFactory, BankRouter, ProcessBankRequest.

public class AcmeFactory : IBankTransactionFactory
{
    public ITransactionStringParser CreateTransactionParser()
    {
        return new AcmeParser();
    }
}
