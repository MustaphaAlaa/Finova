using Npgsql;
using NpgsqlTypes;
using Transactions.Contracts.Entities;
using Transactions.Contracts.Interfaces;

namespace Transactions.Infrastructe.EntitiesConfigurations;

public sealed class BankTransactionStager(NpgsqlDataSource dataSource) : IBankTransactionStager
{
    private const string CreateStageTableSql = """
        CREATE TEMP TABLE bank_transaction_stage_temp
        (LIKE transactions.bank_transaction_stage INCLUDING DEFAULTS)
        ON COMMIT DROP;
        """;

    private const string CopySql = """
        COPY bank_transaction_stage_temp
            ("Id", "WebhookDeliveryId", "TransactionId",
             "Source", "Destination", "Currency",
             "Amount", "Reference", "Date",
             "StagedAtUtc", "InsertedAtUtc", "BankId", "Status", "Metadata")
        FROM STDIN (FORMAT BINARY)
        """;

    private const string InsertSql = """
        INSERT INTO transactions.bank_transaction_stage
            ("Id", "WebhookDeliveryId", "TransactionId",
             "Source", "Destination", "Currency",
             "Amount", "Reference", "Date",
             "StagedAtUtc", "InsertedAtUtc", "BankId", "Status", "Metadata")
        SELECT
            "Id", "WebhookDeliveryId", "TransactionId",
            "Source", "Destination", "Currency",
            "Amount", "Reference", "Date",
            "StagedAtUtc", "InsertedAtUtc", "BankId", "Status", "Metadata"
        FROM bank_transaction_stage_temp
        ON CONFLICT ("TransactionId", "Reference") DO NOTHING;
        """;

    public async Task<int> StageAsync(
        IReadOnlyCollection<BankTransaction> items,
        CancellationToken ct
    )
    {
        if (items.Count == 0)
            return 0;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);
        try
        {
            await using (var command = new NpgsqlCommand(CreateStageTableSql, conn, transaction))
            {
                await command.ExecuteNonQueryAsync(ct);
            }

            await using (var writer = await conn.BeginBinaryImportAsync(CopySql, ct))
            {
                foreach (var t in items)
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(t.Id, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(t.WebhookDeliveryId, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(t.TransactionId, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(t.Source, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(t.Destination, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync((int)t.Currency, NpgsqlDbType.Integer, ct);
                    await writer.WriteAsync(t.Amount, NpgsqlDbType.Numeric, ct);
                    await writer.WriteAsync(t.Reference, NpgsqlDbType.Text, ct);

                    // Npgsql requires Kind=Utc for timestamptz.
                    var date =
                        t.Date.Kind == DateTimeKind.Utc
                            ? t.Date
                            : DateTime.SpecifyKind(t.Date, DateTimeKind.Utc);
                    await writer.WriteAsync(date, NpgsqlDbType.TimestampTz, ct);
                    await writer.WriteAsync(DateTime.UtcNow, NpgsqlDbType.TimestampTz, ct);
                    await writer.WriteNullAsync(ct);
                    await writer.WriteAsync(t.BankId, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync((short)0, NpgsqlDbType.Smallint, ct);
                    await writer.WriteAsync(t.Metadata, NpgsqlDbType.Jsonb, ct);
                }

                await writer.CompleteAsync(ct);
            }

            int inserted;
            await using (var command = new NpgsqlCommand(InsertSql, conn, transaction))
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
}
