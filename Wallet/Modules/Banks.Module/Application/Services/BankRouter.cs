using Banks.Application.Interfaces.IFactory;
using Microsoft.Extensions.DependencyInjection;

namespace Banks.Application.Services;

public class BankRouter(IServiceProvider service)
{
    public IBankServicesFactory ProcessBankRequest(string bankCode)
    {
        // Dynamically resolve the right service implementation at runtime

        var bankServicesFactory = service.GetKeyedService<IBankServicesFactory>(bankCode);
        return bankServicesFactory;
    }
}