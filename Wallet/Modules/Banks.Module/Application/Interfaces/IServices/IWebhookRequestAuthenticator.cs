using Banks.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Banks.Application.Interfaces.IServices;


public interface IWebhookRequestAuthenticator
{
    WebhookSignatureInputs BuildWebhookSignatureValidationInputs();
    bool TryPrepareWebhookSignatureValidation(AuthorizationFilterContext context);
}
