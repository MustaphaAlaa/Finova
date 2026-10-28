using Banks.Application.Extensions;
using Banks.Infrastructure.Extensions;
using Identity.Infrastructure;
using Shared.Extensions;
using StackExchange.Redis;
using Transaction.Infrastructure;
using Transactions.Application.Extensions;

namespace Finova.API.Extensions;
//
// public static partial class FinovaExtensions
// {
//     public static IServiceCollection AddFinovaServices(
//         this IServiceCollection services,
//         IConfiguration configuration
//     ) 
//        
//         using var loggerFactory = LoggerFactory.Create(builder =>
//         {
//             builder.AddConfiguration(configuration.GetSection("Logging"));
//             builder.AddConsole();
//             builder.AddDebug();
//         });
//
//         AddFinovaPolicies(services);
//
//         return services;
//     } 
// }






public static partial class FinovaExtensions
{
    public static IServiceCollection AddFinovaServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = GetRequiredConnectionString(
            configuration,
            "finovaWalletDb");

        var redisConnectionString = GetRequiredConnectionString(
            configuration,
            "Redis");

        AddRedisServices(services, redisConnectionString);
        AddModuleServices(services, connectionString);
        AddFinovaPolicies(services);

        return services;
    }

    private static void AddRedisServices(
        IServiceCollection services,
        string redisConnectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "finovaIdempotency:";
        });
    }

    private static void AddModuleServices(
        IServiceCollection services,
        string connectionString)
    {
        services.AddIdentityModuleInfrastructure(connectionString);
        services.AddSharedExtensions(connectionString);

        services.AddBanksInfrastructure(connectionString);
        services.AddBanksApplicationServices();

        services.AddTransactionsInfrastructure(connectionString);
        services.AddTransactionsApplicationServices();
    }

    private static string GetRequiredConnectionString(
        IConfiguration configuration,
        string name)
    {
        string? connectionString = configuration.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{name}' is required.");

        return connectionString;
    }

    public static async Task MigrateFinovaDatabasesAsync(
        this IServiceProvider servicesProvider,
        CancellationToken cancellationToken = default)
    {
        await servicesProvider.MigrateBanksInfrastructureAsync(
            cancellationToken);

        await servicesProvider.MigrateIdentityInfrastructureAsync(
            cancellationToken);

        await servicesProvider.MigrateTransactionsInfrastructureAsync(
            cancellationToken);
    }
}