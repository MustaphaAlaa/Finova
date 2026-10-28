using System.Collections.Concurrent;
using Banks.Contracts.Entities;
using Shared.Infrastructure.Interfacse.IRepositrories;
using Transaction.Contracts.Entities;
using Transaction.Contracts.Enums;

namespace Transaction.Contracts;

public interface ITransactionRepository : IBaseRepository<BankTransaction>
{
    public Task<List<BankTransaction>> GetTransactionsByEventType(
        string eventType,
        bool sentTransaction = false,
        bool allTransactions = false,
        CancellationToken cancellationToken = default
    );

    BankTransaction GetTransactionsById(
        Guid transactionId,
        CancellationToken cancellationToken = default
    );

    Task WriteMessageProcessingRecordAsync(
        InboundWebhookMessage message,
        TransactionMessageProcessingStatus status,
        string? reason,
        CancellationToken cancellationToken
    );

    Task<int> WriteTransactionsProcessingRecordAsync(
        ConcurrentBag<TransacitonProcessingRecord> records,
        CancellationToken ct
    );

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
