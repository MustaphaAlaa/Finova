using Banks.Application.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Banks.Application.Interfaces;

public interface IBankTransactionWebhookValidationServices
{
    WebhookHeaders GetWebhookHeadersBytes();
    bool ValidateRequestHeaders(AuthorizationFilterContext context);
}
