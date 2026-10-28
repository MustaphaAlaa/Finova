using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Shared.Interfaces.IServices;

public interface IRequestUtilities
{
    string? GetRouteParameterValue(
        AuthorizationFilterContext context,
        IActionResult? contextResult,
        string routeParameter
    );

    string? GetRouteParameterValue(
        ActionExecutingContext context,
        IActionResult? contextResult,
        string routeParameter
    );
}
