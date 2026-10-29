using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Shared.Interfaces.IServices;

[assembly: InternalsVisibleTo("Shared.UnitTests")]
namespace Shared.Services;

internal class RequestUtilities : IRequestUtilities
{
    public string? GetRouteParameterValue(
        AuthorizationFilterContext context,
        IActionResult? contextResult,
        string routeParameter
    )
    {
        return GetRouteParameterValue(context.RouteData.Values, routeParameter, contextResult);
    }

    public string? GetRouteParameterValue(
        ActionExecutingContext context,
        IActionResult? contextResult,
        string routeParameter
    )
    {
        return GetRouteParameterValue(context.RouteData.Values, routeParameter, contextResult);
    }

    private string? GetRouteParameterValue(
        RouteValueDictionary routeValues,
        string routeParameter,
        IActionResult context
    )
    {
        if (
            !routeValues.TryGetValue(routeParameter, out var routeValue)
            || routeValue is not string bankCode
            || string.IsNullOrWhiteSpace(bankCode)
        )
        {
            context = new BadRequestObjectResult("Missing or invalid bank code.");

            return null;
        }

        return bankCode;
    }
}