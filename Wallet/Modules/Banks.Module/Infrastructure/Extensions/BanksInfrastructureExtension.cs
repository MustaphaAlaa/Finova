using Banks.Application.Interfaces.IRepositories;
using Banks.Infrastructure.Outbox;
using Banks.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        
        services.AddDbContext<BanksDbContext>(opt => { opt.UseNpgsql(connectionString); });
    }
}