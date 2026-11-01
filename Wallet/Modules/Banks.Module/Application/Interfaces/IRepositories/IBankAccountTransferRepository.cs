using Banks.Domain.Entities;

namespace Banks.Application.Interfaces.IRepositories;

public interface IBankAccountTransferRepository
{
    Task<BankAccount?> GetOwnedBankAccountForTransferAsync(
        Guid accountId,
        Guid applicationUserId,
        CancellationToken cancellationToken);
}
