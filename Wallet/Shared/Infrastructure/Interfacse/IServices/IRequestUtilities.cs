using Microsoft.AspNetCore.Mvc.Filters;

namespace Shared.Services.Interfaces;

public interface IRequestUtilities
{
    string GetRouteParameterValue(AuthorizationFilterContext context, string routeParameter);
}
