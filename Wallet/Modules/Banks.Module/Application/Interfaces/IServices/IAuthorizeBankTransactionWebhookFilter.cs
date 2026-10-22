using Microsoft.AspNetCore.Mvc.Filters;

namespace Banks.Application.Interfaces;

public interface IAuthorizeBankTransactionWebhookFilter : IAsyncAuthorizationFilter
{
}