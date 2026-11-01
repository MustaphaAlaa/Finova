using Banks.Application.Interfaces.IRepositories;
using Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banks.Infrastructure.Repositories;

public sealed class BankAccountTransferRepository(BanksDbContext dbContext)
    : IBankAccountTransferRepository
{
    public Task<BankAccount?> GetOwnedBankAccountForTransferAsync(
        Guid accountId,
        Guid applicationUserId,
        CancellationToken cancellationToken) =>
        dbContext.BankAccounts
            .Include(account => account.Currency)
            .Include(account => account.Balances)
            .SingleOrDefaultAsync(
                account => account.Id == accountId && account.ApplicationUserId == applicationUserId,
                cancellationToken);
}
