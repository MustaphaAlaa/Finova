using Banks.Application.Interfaces.IRepositories;
using Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Repositories;

namespace Banks.Infrastructure.Repositories;

internal sealed class BankRepository(BanksDbContext dbContext)
    : BaseRepository<Bank, BanksDbContext>(dbContext),
        IBankRepository
{
    public async Task<Bank> GetBankByName(Domain.Entities.ContractedBanks bank)
    {
        var res = await Db.Banks.SingleOrDefaultAsync(b => b.Name == bank);
        return res;
    }

    public async Task<bool> IsContractedBank(Guid id)
    {
        var res = await Db.Banks.AnyAsync(b => b.Id == id && b.Contracted);
        return res;
    }
}
