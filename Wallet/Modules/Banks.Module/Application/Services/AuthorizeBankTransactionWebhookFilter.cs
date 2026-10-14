using System.Diagnostics;
using Finova.Modules.Banks.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Finova.Modules.Banks.Application.Services;

internal sealed class AuthorizeBankTransactionWebhookFilter(
    IBankWebhooksServices webhookFilterServices,
    ILogger<AuthorizeBankTransactionWebhookFilter> logger
) : IAuthorizeBankTransactionWebhookFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        await webhookFilterServices.OnAuthorizationAsync(context);
        stopwatch.Stop();

 
        logger.LogInformation(
            "Webhook authorization filter completed. "
                + "ElapsedMs: {ElapsedMs}, RequestId: {RequestId}",
            stopwatch.ElapsedMilliseconds,
            context.HttpContext.TraceIdentifier
        );
    }
}
