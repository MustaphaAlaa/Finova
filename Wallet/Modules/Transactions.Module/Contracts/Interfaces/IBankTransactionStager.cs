using Transactions.Contracts.Entities;

namespace Transactions.Contracts.Interfaces;

public interface IBankTransactionStager 
{
    Task<int> StageAsync(IReadOnlyCollection<BankTransaction> items, CancellationToken ct);
}