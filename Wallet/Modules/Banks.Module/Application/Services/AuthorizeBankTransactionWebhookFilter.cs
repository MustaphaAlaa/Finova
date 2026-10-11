using Microsoft.AspNetCore.Mvc.Filters;

namespace Finova.Modules.Banks.Application.Services;

public sealed class AuthorizeBankTransactionWebhookFilter(BankTransactionWebhookServices validReq)
    : IAsyncActionFilter
{
    private const string WebhookSignatureHeader = "X-Webhook-Signature";

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        var authorized = await validReq.AuthorizeWebhook(context, WebhookSignatureHeader);
        if (!authorized)
            return;

        await next();
    }
}
