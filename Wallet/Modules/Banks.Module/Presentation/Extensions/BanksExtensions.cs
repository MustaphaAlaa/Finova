using Finova.Modules.Banks.Application.Services;
using Finova.Modules.Banks.Infrastructure.Extensions;
using Finova.Modules.Banks.Presentation.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Finova.Modules.Banks.Extensions;

public static class BanksExtensions
{
    public static void AddBanksExtensions(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<BankTransactionWebhookServices>();
        services.AddBanksInfrastructure(connectionString);
    }
}
