using Banks.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Banks.Application.Services.WebhookFilter;

internal sealed class BankTransactionWebhookAuthorizationAuthorizationFilter(
    IBankWebhookAuthorizationService webhookFilterServices,
    ILogger<BankTransactionWebhookAuthorizationAuthorizationFilter> logger
) : IBankTransactionWebhookAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        await webhookFilterServices.AuthorizeWebhookRequestAsync(context);

        logger.LogInformation(
            "Webhook authorization filter completed. "
            + "RequestId: {RequestId}",
            context.HttpContext.TraceIdentifier
        );
    }
}