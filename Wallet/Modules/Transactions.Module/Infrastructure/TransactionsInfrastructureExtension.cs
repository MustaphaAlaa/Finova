using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Transaction.Contracts;
using Transaction.Contracts.Interfaces;
using Transaction.Infrastructure.Repositories;

namespace Transaction.Infrastructure;

public static class TransactionsInfrastructureExtension
{
    public static void AddTransactionsInfrastructure(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddSingleton<NpgsqlDataSource>(_ =>
        NpgsqlDataSource.Create(connectionString));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IBankTransactionStager, BankTransactionStager>();
        services.AddScoped<IBankTransactionStageFlusher, BankTransactionStageFlusher>();

        services.AddDbContext<TransactionsDbContext>(opt => opt.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsAssembly(
                typeof(TransactionsDbContext).Assembly.FullName)
        ));
    }

    public static async Task MigrateTransactionsInfrastructureAsync(
        this IServiceProvider servicesProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = servicesProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Transactions.Migrations");

        logger.LogInformation("Applying Transactions database migrations.");

        await dbContext.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Transactions database migrations applied.");
    }
}