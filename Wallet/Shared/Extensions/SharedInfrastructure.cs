using IdentityModule.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Extensions;

internal static class SharedInfrastructure
{
    public static void AddSharedInfrastructure(
        this IServiceCollection services,
        string connectionString
    )
    {
        // services
        //     .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        //     {
        //         options.User.RequireUniqueEmail = true;

        //         // options.Password.RequiredLength = 6;
        //         options.Password.RequireNonAlphanumeric = false;
        //         options.Password.RequireUppercase = false;
        //         options.Password.RequireLowercase = false;
        //         // options.Password.RequireDigit = true;
        //     })
        //     .AddEntityFrameworkStores<SharedDbContext>()
        //     .AddDefaultTokenProviders();

        // services.AddDbContext<SharedDbContext>(opt =>
        // {
        //     opt.UseNpgsql(connectionString);
        // });
    }
}
