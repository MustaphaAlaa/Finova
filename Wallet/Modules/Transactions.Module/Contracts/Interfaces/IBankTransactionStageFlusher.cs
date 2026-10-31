namespace Transaction.Contracts.Interfaces;

public interface IBankTransactionStageFlusher
{
    Task<long> FlushOnceAsync(int batchSize = 5_000, CancellationToken ct = default);
}
