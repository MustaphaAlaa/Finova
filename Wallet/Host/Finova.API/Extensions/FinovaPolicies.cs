using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Timeouts;

namespace Finova.API.Extensions;

public static partial class FinovaExtensions
{
    private static IServiceCollection AddFinovaPolicies(
        IServiceCollection services)
    {
        AddCorsPolicy(services);
        AddRateLimitingPolicy(services);
        AddRequestTimeoutPolicies(services);

        return services;
    }

    private static void AddCorsPolicy(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("allowAll", policy =>
            {
                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });
        });
    }

    private static void AddRateLimitingPolicy(
        IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter =
                PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var partitionKey =
                        httpContext.User.Identity?.Name
                        ?? httpContext.Request.Headers.Host.ToString();

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 30_000,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });
        });
    }

    private static void AddRequestTimeoutPolicies(
        IServiceCollection services)
    {
        services.AddRequestTimeouts(options =>
        {
            options.DefaultPolicy = new RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(15) };

            options.AddPolicy(
                "Long",
                TimeSpan.FromSeconds(40));
        });
    }
}