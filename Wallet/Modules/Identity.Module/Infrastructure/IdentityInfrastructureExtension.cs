using Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure;

public static class IdentityInfrastructureExtension
{
    public static void AddIdentityModuleInfrastructure(
        this IServiceCollection services,
        string connectionString
    )
    {
        

        // Configure Identity
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                // options.Password.RequireDigit = true;
            })
            .AddEntityFrameworkStores<IdentityModuleDbContext>();

        services.AddDbContext<IdentityModuleDbContext>(opt =>
        {
            opt.UseNpgsql(connectionString);
        });
    }

    public static async Task MigrateIdentityInfrastructureAsync(
        this IServiceProvider servicesProvider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = servicesProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Identity.Migrations");

        logger.LogInformation("Applying Identity database migrations.");
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Identity database migrations applied.");
    }
}
