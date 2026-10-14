using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Extensions;
using Shared.Services.Interfaces;

namespace Finova.Shared.Extensions;

public static class SharedExtensions
{
    public static void AddSharedExtensions(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddSharedInfrastructure(connectionString);
        services.AddScoped<IRequestUtilities, RequestUtilities>();
    }
};
