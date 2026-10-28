using System.Collections.Concurrent;
using Transaction.Contracts.Entity;

namespace Transactions.Application.Interfaces.IValidations;

public interface ITransactionServicesValidator
{
    void ValidateTransactionRequest(TransactionsRequest request);
    Task<ConcurrentBag<TransactionsRequest>> ValidateTransactionRequestsAsync(
        List<TransactionsRequest> requests,
        CancellationToken cancellationToken
    );
}