using Microsoft.Extensions.DependencyInjection;
using Shared.Exceptions;
using Shared.Interfaces.IServices;
using Shared.Services;

namespace Shared.Extensions;

public static class SharedExtensions
{
    public static void AddSharedExtensions(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddSharedInfrastructure(connectionString);

        services.AddScoped<IRequestUtilities, RequestUtilities>();

        services.AddExceptionHandler<InternalConfigurationExceptionHandler>();
    }
};