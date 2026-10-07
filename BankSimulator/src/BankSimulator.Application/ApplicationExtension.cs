using Microsoft.Extensions.DependencyInjection;

namespace BankSimulator.Application.Extension;

public static class ApplicationExtension
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IWebhookSubscriptionServices, WebhookSubscriptionServices>();
        services.AddScoped<IWebhookSubscriptionValidation, WebhookSubscriptionValidation>();
    }
}
