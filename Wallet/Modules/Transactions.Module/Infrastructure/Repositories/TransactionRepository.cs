using System.Collections.Concurrent;
using Banks.Contracts.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Shared.Infrastructure.Repositories;
using Transaction.Contracts.Enums;
using Transactions.Contracts.Entities;

namespace Transactions.Infrastructe;

public sealed class TransactionRepository(
    NpgsqlDataSource dataSource,
    TransactionsDbContext dbContext
) : BaseRepository<BankTransaction, TransactionsDbContext>(dbContext), ITransactionRepository
{
    public Task<List<BankTransaction>> GetTransactionsByEventType(
        string eventType,
        bool sentTransaction = false,
        bool allTransactions = false,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public BankTransaction GetTransactionsById(
        Guid transactionId,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public async Task WriteMessageProcessingRecordAsync(
        InboundWebhookMessage message,
        TransactionMessageProcessingStatus status,
        string? reason,
        CancellationToken cancellationToken
    )
    {
        await Db.MessageProcessingRecords.AddAsync(
            new MessageProcessingRecord
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                Status = status,
                Reason = reason,
                ProcessedAtUtc = DateTime.UtcNow,
            },
            cancellationToken
        );
    }
   
    public async Task SetMessageStatusAsync(
        InboundWebhookMessage message,
        TransactionMessageProcessingStatus status,
        string? reason,
        CancellationToken cancellationToken
    )
    {
        await Db.AddAsync(
            new MessageProcessingRecord
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                Status = status,
                Reason = reason,
                ProcessedAtUtc = DateTime.UtcNow,
            },
            cancellationToken
        );
    }

    public async Task<int> WriteTransactionsProcessingRecordAsync(
        ConcurrentBag<TransacitonProcessingRecord> records,
        CancellationToken ct
    )
    {
        await using var con = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await con.BeginTransactionAsync(ct);
        try
        {
            const string createSql = """
                CREATE TEMP TABLE processing_records_stage
                (LIKE transactions."TransacitonProcessingRecords"
                 INCLUDING DEFAULTS)
                ON COMMIT DROP;
                """;

            await using (var command = new NpgsqlCommand(createSql, con, transaction))
            {
                await command.ExecuteNonQueryAsync(ct);
            }

            string copySql = """ 
                    COPY  processing_records_stage
                        ("Id","TransactionId", "Status", "Reason", "ProcessedAtUtc")
                   FROM STDIN (FORMAT BINARY)

                """;

            await using (var writer = await con.BeginBinaryImportAsync(copySql, ct))
            {
                foreach (var record in records)
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(Guid.NewGuid(), NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(record.TransactionId, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync((int)record.Status, NpgsqlDbType.Integer, ct);
                    await writer.WriteAsync(record.Reason, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(record.ProcessedAtUtc, NpgsqlDbType.TimestampTz, ct);
                }

                await writer.CompleteAsync(ct);
            }

            const string insertSql = """
                INSERT INTO transactions."TransacitonProcessingRecords"
                    ("Id", "TransactionId", "Status", "Reason", "ProcessedAtUtc")
                SELECT
                    "Id", "TransactionId", "Status", "Reason", "ProcessedAtUtc"
                FROM processing_records_stage
                ON CONFLICT ("TransactionId") DO NOTHING;
                """;

            int inserted;

            await using (var command = new NpgsqlCommand(insertSql, con, transaction))
            {
                inserted = await command.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);

            return inserted;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await Db.SaveChangesAsync(cancellationToken);
    }
}
