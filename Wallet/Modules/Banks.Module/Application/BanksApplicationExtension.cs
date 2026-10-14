using Finova.Modules.Banks.Application.Interfaces;
using Finova.Modules.Banks.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Finova.Modules.Banks.Extensions;

public static class BanksApplicationExtension
{
    public static void AddBanksApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<
            IBankTransactionWebhookValidationServices,
            BankTransactionWebhookValidationServices
        >();

        services.AddScoped<
            IAuthorizeBankTransactionWebhookFilter,
            AuthorizeBankTransactionWebhookFilter
        >();

        services.AddScoped<IBankWebhooksServices, BankWebhooksServices>();
    }
}
