using Microsoft.Extensions.DependencyInjection;
using Transactions.Application.Interfaces.IFactory;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class BankTransactionParserResolver(IServiceProvider service)
{
    public IBankTransactionFactory ResolveFactory(string bankCode)
    {
        // Dynamically resolve the right service implementation at runtime

        var factoryServices = service.GetKeyedService<IBankTransactionFactory>(bankCode);
        return factoryServices;
    }
}