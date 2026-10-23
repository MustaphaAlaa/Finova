using Banks.Application.Exceptions;
using Banks.Application.Interfaces;
using Banks.Application.Interfaces.IServices;
using Banks.Application.Interfaces.IValidations;
using Banks.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Banks.Application.Extensions;

public static class BanksApplicationExtension
{
    public static void AddBanksApplicationServices(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<InvalidWebhookRequestExceptionHandler>();
        services.AddScoped<
            IBankTransactionWebhookValidationServices,
            BankTransactionWebhookValidationServices
        >();

        services.AddScoped<
            IAuthorizeBankTransactionWebhookFilter,
            AuthorizeBankTransactionWebhookFilter
        >();

        services.AddScoped<IBankWebhooksServices, BankWebhooksServices>();
        services.AddScoped<IBankServices, BankServices>();
        services.AddScoped<IBankTransactionWebhookValidator,BankTransactionWebhookValidator>();
    }
}
