using Microsoft.AspNetCore.Mvc.Filters;

namespace Banks.Application.Interfaces;

public interface IBankWebhooksServices
{
    Task OnAuthorizationAsync(AuthorizationFilterContext context);
}
