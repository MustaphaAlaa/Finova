using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Services.Interfaces;

internal class RequestUtilities : IRequestUtilities
{
    public string GetRouteParameterValue(AuthorizationFilterContext context, string routeParameter)
    {
        if (
            !context.RouteData.Values.TryGetValue(routeParameter, out var routeValue)
            || routeValue is not string bankCode
            || string.IsNullOrWhiteSpace(bankCode)
        )
        {
            context.Result = new BadRequestObjectResult("Missing or invalid bank code.");
        }

        return routeValue.ToString();
    }
}
