using Banks.Extensions;
using Finova.Shared.Extensions;

namespace Finova.API.Extensions;

public static partial class FinovaExtensions
{
    public static IServiceCollection AddFinovaServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.AddDebug();
        });

        var connectionString = configuration.GetConnectionString("finovaWalletDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            loggerFactory.CreateLogger("Please set connection string in appsettings.json");
            throw new Exception("There's an error occurred right now in our servers");
        }

        services.AddSharedExtensions(connectionString);
        services.AddBanksExtensions(connectionString);

        AddFinovaPolicies(services);

        return services;
    }
}