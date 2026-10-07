using BankSimulator.Application;
using BankSimulator.Domain;
using Microsoft.EntityFrameworkCore;

namespace BankSimulator.Infrastructure.Repositories;

public sealed class TransactionWebhookRepository(BankSimulatorDbContext dbContext)
    : ITransactionWebhookRepository
{
    public async Task<IReadOnlyList<TransactionWebhookSubscription>> GetSubscriptionsAsync(
        Guid clientId,
        string eventType,
        CancellationToken cancellationToken
    ) =>
        await dbContext
            .TransactionWebhookSubscriptions.Where(subscription =>
                subscription.ClientId == clientId && subscription.EventType == eventType
            )
            .ToListAsync(cancellationToken);

    public void AddOutboxMessage(WebhookOutboxMessage message) =>
        dbContext.WebhookOutboxMessages.Add(message);

    public void AddWebhook(TransactionWebhookSubscription webhookSubscription) =>
        dbContext.TransactionWebhookSubscriptions.Add(webhookSubscription);

    public async Task<int> SaveChangesAsync() => await dbContext.SaveChangesAsync();
}
