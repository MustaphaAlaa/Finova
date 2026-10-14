using Microsoft.AspNetCore.Mvc.Filters;

namespace Finova.Modules.Banks.Application.Interfaces;

public interface IAuthorizeBankTransactionWebhookFilter : IAsyncAuthorizationFilter { }
