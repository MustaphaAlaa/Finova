using Banks.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Banks.Presentation.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuthorizeBankTransactionWebhookAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var authWebhookFilter =
            serviceProvider.GetRequiredService<IBankTransactionWebhookAuthorizationFilter>();

        return authWebhookFilter;
    }
}