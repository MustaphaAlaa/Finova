using Banks.Application.Extensions;
using Banks.Infrastructure.Extensions;
using Finova.Shared.Extensions;
using IdentityModule.Infrastructure.Extensions;
using Transactions.Infrastructure.Extensions;

namespace Finova.API.Extensions;

public static partial class FinovaExtensions
{
    public static IServiceCollection AddFinovaServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("finovaWalletDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'finovaWalletDb' is required to apply database migrations.");
        }

        services.AddIdentityModuleInfrastructure(connectionString);
        services.AddSharedExtensions(connectionString);

        services.AddBanksInfrastructure(connectionString);
        services.AddBanksApplicationServices();

        services.AddTransactionsInfrastructure(connectionString);


        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.AddDebug();
        });


        AddFinovaPolicies(services);

        return services;
    }

    public static async Task MigrateFinovaDatabasesAsync(
        this IServiceProvider servicesProvider,
        CancellationToken cancellationToken = default)
    {
        await servicesProvider.MigrateBanksInfrastructureAsync(cancellationToken);
        await servicesProvider.MigrateIdentityInfrastructureAsync(cancellationToken);
        await servicesProvider.MigrateTransactionsInfrastructureAsync(cancellationToken);
    }
}