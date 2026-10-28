namespace Transaction.Contracts.Interfaces;

public interface IBankTransactionStageFlusher
{
    Task<long> FlushOnceAsync(CancellationToken ct, int batchSize = 5_000);
}
