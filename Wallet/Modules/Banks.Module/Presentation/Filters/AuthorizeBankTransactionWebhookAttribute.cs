using Finova.Modules.Banks.Application.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Finova.Modules.Banks.Presentation.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuthorizeBankTransactionWebhookAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var validReq = serviceProvider.GetRequiredService<BankTransactionWebhookServices>();

        return new AuthorizeBankTransactionWebhookFilter(validReq);
    }
}
