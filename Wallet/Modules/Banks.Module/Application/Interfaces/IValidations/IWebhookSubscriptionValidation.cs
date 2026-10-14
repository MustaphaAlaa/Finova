namespace Finova.Modules.Banks.Application;

public interface IWebhookSubscriptionValidation
{
    Result<string> ValidateWebhookRequest(CreateTransactionWebhookRequest webhookRequest);
}
