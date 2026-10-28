using Banks.Application.Exceptions;
using Banks.Application.Interfaces;
using Banks.Application.Interfaces.IServices;
using Banks.Application.Interfaces.IValidations;
using Banks.Application.Services;
using Banks.Application.Services.WebhookFilter;
using Microsoft.Extensions.DependencyInjection;

namespace Banks.Application.Extensions;

public static class BanksApplicationExtension
{
    public static void AddBanksApplicationServices(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<InvalidWebhookRequestExceptionHandler>();

        services.AddScoped<WebhookAuthenticationConfiguration>();

        services.AddScoped<
            IWebhookRequestAuthenticator,
            WebhookRequestAuthenticator
        >();

        services.AddScoped<
            IBankTransactionWebhookAuthorizationFilter,
            BankTransactionWebhookAuthorizationAuthorizationFilter
        >();

        services.AddScoped<IBankWebhookAuthorizationService, BankWebhookAuthorizationService>();
        services.AddScoped<IBankServices, BankServices>();
        services.AddScoped<IWebhookDeliveryValidator, WebhookDeliveryValidator>();
    }
}