using System.Collections.Concurrent;
using Transactions.Contracts.Entities;

public interface ITransactionServicesValidator
{
    void ValidateTransactionRequest(TransactionsRequest request);
    Task<ConcurrentBag<TransactionsRequest>> ValidateTransactionRequestsAsync(
        List<TransactionsRequest> requests,
        CancellationToken cancellationToken
    );
}
