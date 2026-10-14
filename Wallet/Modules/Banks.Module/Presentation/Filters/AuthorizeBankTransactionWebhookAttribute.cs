using Finova.Modules.Banks.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Finova.Modules.Banks.Presentation.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuthorizeBankTransactionWebhookAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var authWebhookFilter =
            serviceProvider.GetRequiredService<IAuthorizeBankTransactionWebhookFilter>();

        return authWebhookFilter;
    }
}
