using Transaction.Contracts.Entities;

namespace Transaction.Contracts.Interfaces;

public interface IBankTransactionStager 
{
    Task<int> StageAsync(IReadOnlyCollection<BankTransaction> items, CancellationToken ct);
}