using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Finova.Modules.Banks.Application.Services;

public sealed class AuthorizeBankTransactionWebhookFilter(
    BankWebhooksServices webhookFilterServices,
    ILogger<AuthorizeBankTransactionWebhookFilter> logger
) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        await webhookFilterServices.OnAuthorizationAsync(context);
        stopwatch.Stop();

        double executionTime = stopwatch.ElapsedMilliseconds / 1000;

        logger.LogInformation(
            $"The {string.Join(", ", context.ActionDescriptor.FilterDescriptors)} take {executionTime} sec to execute"
        );
    }
}
