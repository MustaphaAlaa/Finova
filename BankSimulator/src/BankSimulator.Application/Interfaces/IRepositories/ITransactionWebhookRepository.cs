using BankSimulator.Domain;

namespace BankSimulator.Application;

public interface ITransactionWebhookRepository
{
    Task<IReadOnlyList<TransactionWebhookSubscription>> GetSubscriptionsAsync(
        Guid clientId,
        string eventType,
        CancellationToken cancellationToken
    );
    void AddOutboxMessage(WebhookOutboxMessage message);
    void AddWebhook(TransactionWebhookSubscription webhookSubscription);
    Task<int> SaveChangesAsync();
}
