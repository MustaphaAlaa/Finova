namespace BankSimulator.Application;

public interface IWebhookSubscriptionServices
{
    Task<Result<string>> AddWebhookSubscription(CreateTransactionWebhookRequest webhookRequest);
}
