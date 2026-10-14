using Finova.Modules.Banks.Application.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Finova.Modules.Banks.Application.Interfaces;

public interface IBankTransactionWebhookValidationServices
{
    WebhookHeaders GetWebhookHeadersBytes();
    bool ValidateRequestHeaders(AuthorizationFilterContext context);
}
