using Transactions.Contracts.Entities;

public interface ITransactionServicesValidator
{
    void ValidateTransactionRequest(TransactionsRequest request);
    Task<List<TransactionsRequest>> ValidateTransactionRequestsAsync(
        List<TransactionsRequest> requests,
        CancellationToken cancellationToken
    );
}
