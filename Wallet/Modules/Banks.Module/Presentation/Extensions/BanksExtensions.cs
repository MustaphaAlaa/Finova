using Banks.Application.Extensions;
using Banks.Application.Services;
using Banks.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Banks.Extensions;

public static class BanksExtensions
{
    public static void AddBanksExtensions(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<WebhookHeaderSettings>();

        services.AddBanksInfrastructure(connectionString);
        services.AddBanksApplicationServices();
    }
}
