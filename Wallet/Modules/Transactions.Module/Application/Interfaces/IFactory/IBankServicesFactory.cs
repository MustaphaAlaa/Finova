namespace Transactions.Application.Interfaces.IFactory;

// I used Abstract Factory Pattern, because if I add another services for banks.
public interface IBankServicesFactory
{
    ITransactionStringParser CreateTransactionParser();
}