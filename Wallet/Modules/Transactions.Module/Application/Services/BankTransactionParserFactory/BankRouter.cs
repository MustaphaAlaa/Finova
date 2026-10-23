using Microsoft.Extensions.DependencyInjection;
using Transactions.Application.Interfaces.IFactory;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class BankRouter(IServiceProvider service)
{
    public IBankServicesFactory ProcessBankRequest(string bankCode)
    {
        // Dynamically resolve the right service implementation at runtime

        var bankServicesFactory = service.GetKeyedService<IBankServicesFactory>(bankCode);
        return bankServicesFactory;
    }
}