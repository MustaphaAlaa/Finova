using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Services.Interfaces;
using StackExchange.Redis;

namespace Banks.Presentation.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class IdempotencyAttribute(IConnectionMultiplexer redis, IRequestUtilities requestUtilities)
    : ActionFilterAttribute
{
    const string IdempotencyKeyHeader = "x-Idempotency-key";
    private readonly IDatabase _database = redis.GetDatabase();
    private const string InProgressStatus = "in-progress";

    private class CacheResponse
    {
        public int StatusCode { get; set; }
        public object? Value { get; set; }
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        if (
            !context.HttpContext.Request.Headers.TryGetValue(
                IdempotencyKeyHeader,
                out var idempotencyKey
            ) || string.IsNullOrWhiteSpace(idempotencyKey)
        )
        {
            context.Result = new BadRequestObjectResult(
                $"missing or empty {IdempotencyKeyHeader} header"
            );
            return;
        }

        var bankCode = requestUtilities.GetRouteParameterValue(context, context.Result, "bankCode");

        var redisKey = $"idempotency:{bankCode}:{idempotencyKey}";

        // var cacheResponseJson = await distributedCache.GetStringAsync(
        //     redisKey,
        //     context.HttpContext.RequestAborted
        // );

        var cacheResponseJson = await _database.StringGetAsync(redisKey);

        // if (!string.IsNullOrWhiteSpace(cacheResponseJson))
        if (cacheResponseJson.HasValue)
        {
            var cacheResponse = JsonSerializer.Deserialize<CacheResponse>(
                cacheResponseJson.ToString()
            );

            if (cacheResponse?.StatusCode == StatusCodes.Status102Processing)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            if (cacheResponse != null)
            {
                context.Result = new ObjectResult(cacheResponse.Value);
                return;
            }
        }
        else
        {
            var progressJson = JsonSerializer.Serialize(
                new CacheResponse
                {
                    StatusCode = (int)StatusCodes.Status102Processing,
                    Value = InProgressStatus,
                }
            );
            await _database.StringSetAsync(
                redisKey,
                progressJson,
                TimeSpan.FromMinutes(3),
                when: When.NotExists
            );
            
            var exectuted = await next();

            if (
                exectuted.Result is ObjectResult objectResult
                && objectResult.StatusCode >= 200
                && objectResult.StatusCode < 300
            )
            {
                var responseCache = new CacheResponse
                {
                    StatusCode = (int)objectResult.StatusCode,
                    Value = objectResult.Value,
                };

                var responseJson = JsonSerializer.Serialize(responseCache);

                await _database.StringSetAsync(
                    redisKey,
                    responseJson,
                    TimeSpan.FromMinutes(3),
                    when: When.Always
                );
            }
        }
    }
}
