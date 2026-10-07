namespace BankSimulator.Application;

public interface IWebhookSubscriptionValidation
{
    Result<string> ValidateWebhookRequest(CreateTransactionWebhookRequest webhookRequest);
}
