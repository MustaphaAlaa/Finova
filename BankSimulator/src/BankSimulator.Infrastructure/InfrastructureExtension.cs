using BankSimulator.Application;
using BankSimulator.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankSimulator.Infrastructure.Extension;

public static class InfrastructureExtension
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITransactionWebhookRepository, TransactionWebhookRepository>();
        services.AddDbContext<BankSimulatorDbContext>(opt =>
        {
            opt.UseNpgsql(configuration.GetConnectionString("BankSimulatorDb"));
        });
    }
}
