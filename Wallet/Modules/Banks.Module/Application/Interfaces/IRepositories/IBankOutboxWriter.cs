using Banks.Domain;
using Finova.Shared.Infrastructure.Interfaces;

namespace Banks.Application.Interfaces.IRepositories;

public interface IBankOutboxWriter : IBaseRepository<InboundWebhookMessage>
{
    Task<List<InboundWebhookMessage>> ClaimBatchAsync(
        int maxRows = 1500,
        int maxBytes = 10 * 1024 * 1024, // ~10 MB per batch
        CancellationToken ct = default);

    Task<int> RequeueStaleProcessingMessagesAsync(
        CancellationToken ct = default);

    Task<bool> IsWebhookDeliveryIdExistAsync(Guid webhookDeliveryId, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}