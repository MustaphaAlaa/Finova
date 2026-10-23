using Banks.Application.Interfaces.IRepositories;
using Banks.Infrastructure.Outbox;
using Banks.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Banks.Infrastructure.Extensions;

public static class BanksInfrastructureExtension
{
    public static void AddBanksInfrastructure(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddScoped<IBankRepository, BankRepository>();
        services.AddScoped<IBankOutboxWriter, BankOutboxWriter>();

        services.AddDbContext<BanksDbContext>(opt => opt.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsAssembly(
                typeof(BanksDbContext).Assembly.FullName)
        ));
    }

    public static async Task MigrateBanksInfrastructureAsync(
        this IServiceProvider servicesProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = servicesProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<BanksDbContext>();

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Banks.Migrations");

        logger.LogInformation("Applying Banks database migrations.");

        await dbContext.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Banks database migrations applied.");
    }
}