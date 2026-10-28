using Microsoft.AspNetCore.Mvc.Filters;

namespace Banks.Application.Interfaces.IServices;

public interface IBankTransactionWebhookAuthorizationFilter : IAsyncAuthorizationFilter
{
}