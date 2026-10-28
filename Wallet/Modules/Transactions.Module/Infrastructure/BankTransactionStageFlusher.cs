using Microsoft.Extensions.Logging;
using Npgsql;
using Transaction.Contracts.Interfaces;

namespace Transaction.Infrastructure;


public sealed class BankTransactionStageFlusher(
    NpgsqlDataSource dataSource,
    ILogger<BankTransactionStageFlusher> logger) : IBankTransactionStageFlusher
{

    private const string FlushSql = """
        WITH batch AS (
            SELECT "Id"
            FROM transactions.bank_transaction_stage
            WHERE "InsertedAtUtc" IS NULL
            ORDER BY "StagedAtUtc"
            LIMIT @batchSize
            FOR UPDATE SKIP LOCKED
        ),
        ins AS (
            INSERT INTO transactions."BankTransactions"
                ("Id", "TransactionId", "WebhookDeliveryId", "Source", "Destination", "Currency",
                 "Amount", "Reference", "Date", "BankId", "Metadata")
            SELECT s."Id", s."TransactionId", s."WebhookDeliveryId",
                   s."Source", s."Destination", s."Currency", s."Amount", s."Reference", s."Date",
                   s."BankId", s."Metadata"
            FROM transactions.bank_transaction_stage s
            JOIN batch b ON b."Id" = s."Id"
            ON CONFLICT ("TransactionId", "Reference") DO NOTHING
            RETURNING 1
        ),
        marked AS (
            UPDATE transactions.bank_transaction_stage s
            SET "InsertedAtUtc" = now(),
                "Status" = 1
            FROM batch b
            WHERE s."Id" = b."Id"
            RETURNING 1
        )
        SELECT (SELECT count(*) FROM marked) AS claimed,
               (SELECT count(*) FROM ins)    AS inserted;
        """;


    public async Task<long> FlushOnceAsync(CancellationToken ct,int batchSize)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(FlushSql, conn);
        cmd.Parameters.AddWithValue("batchSize", batchSize);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);

        var claimed = reader.GetInt64(0);
        var inserted = reader.GetInt64(1);

        if (claimed > 0)
            logger.LogInformation(
                "Flushed {Claimed} staged rows: {Inserted} inserted, {Skipped} duplicates skipped",
                claimed, inserted, claimed - inserted);

        return claimed;
    }
}