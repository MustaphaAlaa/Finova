using Banks.Application.Interfaces.IRepositories;
using Banks.Domain;
using Finova.Shared.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Repositories;

namespace Banks.Infrastructure.Outbox;

class BankOutboxWriter(
    ILogger<BankOutboxWriter> logger,
    BanksDbContext dbContext) : BaseRepository<InboundWebhookMessage, BanksDbContext>(dbContext), IBankOutboxWriter
{
    public async Task<List<InboundWebhookMessage>> ClaimBatchAsync(
        int maxRows = 1500,
        int maxBytes = 10 * 1024 * 1024, // ~10 MB per batch
        CancellationToken ct = default)
    {
        var messages = await Db.InboundWebhookMessages
            .FromSqlInterpolated($"""
                                  WITH picked AS (
                                      SELECT id FROM (
                                          SELECT "InboundWebhookMessageId" AS id,
                                                 "PayloadSizeBytes" AS size,
                                                 SUM("PayloadSizeBytes") OVER (
                                                     ORDER BY "ReceivedAtUtc", "InboundWebhookMessageId") AS running
                                          FROM "InboundWebhookMessages"
                                          WHERE "Status" = 0                       -- Pending
                                            AND "NextAttemptAtUtc" <= now()
                                          ORDER BY "ReceivedAtUtc", "InboundWebhookMessageId"
                                          LIMIT {maxRows}
                                      ) t
                                      WHERE t.running - t.size < {maxBytes}        -- always takes at least 1 row
                                  ) 
                                  UPDATE "InboundWebhookMessages" m
                                  SET "Status" = 1,                                -- Processing
                                      "LockedAtUtc" = now()
                                  WHERE m."InboundWebhookMessageId" IN (
                                      SELECT "InboundWebhookMessageId"
                                      FROM "InboundWebhookMessages"
                                      WHERE "InboundWebhookMessageId" IN (SELECT id FROM picked)
                                      FOR UPDATE SKIP LOCKED
                                  )
                                  RETURNING m.*
                                  """)
            .AsNoTracking()
            .ToListAsync(ct);

        if (messages.Count > 0)
        {
            var totalBytes = messages.Sum(m => m.PayloadSizeBytes);

            logger.LogDebug(
                $"Claimed {messages.Count} inbound webhook messages for processing. Total payload size: {totalBytes} bytes. Configured limits: {maxRows} rows, {maxBytes} bytes.");
        }

        return messages;
    }

    public async Task<int> RequeueStaleProcessingMessagesAsync(
        CancellationToken ct = default)
    {
        var staleCutoffTime = DateTime.UtcNow.AddMinutes(-5);

        var affectedRows = await Db.InboundWebhookMessages.Where(msg =>
                msg.Status == InboundTransactionWebhookOutboxMessageStatus.Processing &&
                msg.LockedAtUtc < staleCutoffTime)
            .ExecuteUpdateAsync(
                setters =>
                {
                    setters
                        .SetProperty(msg => msg.Status, InboundTransactionWebhookOutboxMessageStatus.Pending)
                        .SetProperty(msg => msg.LockedAtUtc, (DateTime?)null);
                }, cancellationToken: ct);

        if (affectedRows > 0)
        {
            logger.LogWarning(
                "Requeued {AffectedRows} stale inbound webhook messages " +
                "that were locked before {StaleCutoffTimeUtc}.",
                affectedRows,
                staleCutoffTime);
        }

        return affectedRows;
    }

    public async Task<bool> IsWebhookDeliveryIdExistAsync(Guid webhookDeliveryId, CancellationToken ct = default)
    {
        return await this.Db.InboundWebhookMessages.AnyAsync(msg => msg.WebhookDeliveryId == webhookDeliveryId, ct);
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken ct = default) => await Db.SaveChangesAsync(ct);
}