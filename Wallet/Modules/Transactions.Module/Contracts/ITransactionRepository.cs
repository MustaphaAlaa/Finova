using System.Collections.Concurrent;
using Banks.Contracts.Entities;
using Finova.Shared.Infrastructure.Interfaces;
using Transaction.Contracts.Enums;
using Transactions.Contracts.Entities;

namespace Transactions.Infrastructe;

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
