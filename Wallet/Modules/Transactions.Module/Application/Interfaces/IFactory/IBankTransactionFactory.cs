using Transactions.Contracts.Entities;

namespace Transactions.Application.Interfaces.IFactory;

// I used Abstract Factory Pattern, because if I add another services for banks.
public interface IBankTransactionFactory
{
    ITransactionStringParser CreateTransactionParser();
}
