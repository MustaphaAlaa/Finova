using System.Collections.Concurrent;
using System.Linq.Expressions;
using Banks.Contracts.Entities;
using Microsoft.Extensions.Logging;
using Transaction.Contracts.Enums;
using Transactions.Contracts.Entities;
using Transactions.Infrastructe;

namespace Transactions.Application.Services;

public class TransactionServicesValidator(
    ITransactionRepository transactionRepository,
    ILogger<TransactionServicesValidator> logger
) : ITransactionServicesValidator
{
    public void ValidateTransactionRequest(TransactionsRequest request)
    {
        if (request.TransactionId == Guid.Empty)
            throw new ArgumentException("TransactionId is required.");

        if (request.Source == Guid.Empty)
            throw new ArgumentException("Source is required.");

        if (request.Destination == Guid.Empty)
            throw new ArgumentException("Destination is required.");

        if (request.Source == request.Destination)
            throw new ArgumentException("Source and destination cannot be the same.");

        if (string.IsNullOrWhiteSpace(request.Currency))
            throw new ArgumentException("Currency is required.");

        if (
            !Enum.TryParse<Currencies>(request.Currency.Trim(), ignoreCase: true, out var currency)
            || !Enum.IsDefined(currency)
        )
        {
            throw new ArgumentException($"Currency '{request.Currency}' is not supported.");
        }

        if (string.IsNullOrWhiteSpace(request.Transaction))
            throw new ArgumentException("Transaction is required.");
    }

    public async Task<ConcurrentBag<TransactionsRequest>> ValidateTransactionRequestsAsync(
        List<TransactionsRequest> requests,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(requests);

        var validRequests = new ConcurrentBag<TransactionsRequest>();
        var failedRecords = new ConcurrentBag<TransacitonProcessingRecord>();

        Parallel.ForEach ( requests,
        new ParallelOptions
        {
            CancellationToken=  cancellationToken,
            MaxDegreeOfParallelism = 3
        }
        , (request) =>
        {
           
            try
            {
                ValidateTransactionRequest(request);
                validRequests.Add(request);
            }
            catch (ArgumentException ex)
            {
                failedRecords.Add(
                    new TransacitonProcessingRecord
                    {
                        TransactionId = request.TransactionId,
                        ProcessedAtUtc = DateTime.UtcNow,
                        Reason = ex.Message,
                        Status = TransactionMessageProcessingStatus.Rejected,
                    }
                );
            } 
        });

        if (failedRecords.Count > 0)
        {
            try
            {
                await transactionRepository.WriteTransactionsProcessingRecordAsync(
                    failedRecords,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to persist {Count} transaction processing records.",
                    failedRecords.Count
                );
                throw;
            }
        }

        return validRequests;
    }
}