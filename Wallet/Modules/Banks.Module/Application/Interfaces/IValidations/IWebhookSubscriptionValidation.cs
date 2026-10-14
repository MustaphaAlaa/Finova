namespace Finova.Modules.Banks.Application.Interfaces;

public interface IWebhookSubscriptionValidation
{
    Result<string> ValidateWebhookRequest(CreateTransactionWebhookRequest webhookRequest);
}
