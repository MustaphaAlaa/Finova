using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finova.Modules.Banks.Infrastructure.Extensions;

public static class BanksInfrastructureExtension
{
    public static void AddBanksInfrastructure(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddDbContext<BanksDbContext>(opt =>
        {
            opt.UseNpgsql(connectionString);
        });
    }
}
