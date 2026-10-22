using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Timeouts;

namespace Finova.API.Extensions;

public static partial class FinovaExtensions
{
    static IServiceCollection AddFinovaPolicies(this IServiceCollection services)
    {
        services.AddCors(opt =>
        {
            opt.AddPolicy(
                "allowAll",
                policy => { policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); }
            );
        });
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name
                                  ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1),
                    }
                )
            );
        });

        services.AddRequestTimeouts(options =>
        {
            options.DefaultPolicy = new RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(15) };
            options.AddPolicy("Long", TimeSpan.FromSeconds(40));
        });
        return services;
    }
}