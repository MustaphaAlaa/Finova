using Microsoft.AspNetCore.Mvc.Filters;

namespace Finova.Modules.Banks.Application.Interfaces;

public interface IBankWebhooksServices
{
    Task OnAuthorizationAsync(AuthorizationFilterContext context);
}
