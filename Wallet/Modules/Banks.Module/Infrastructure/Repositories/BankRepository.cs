using Finova.Modules.Banks.Application.Interfaces.Repositories;
using Finova.Modules.Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Repositories;

namespace Finova.Modules.Banks.Infrastructure.Repositories;

internal sealed class BankRepository(BanksDbContext dbContext)
    : BaseRepository<Bank, BanksDbContext>(dbContext),
        IBankRepository
{
    public async Task<Bank> GetBankByName(Domain.Entities.Banks bank)
    {
        var res = await dbContext.Banks.SingleOrDefaultAsync(b => b.Name == bank.ToString());
        return res;
    }

    public async Task<Bank> ASD()
    {
        var r = await dbContext.Banks.OrderBy(t => EF.Functions.Random()).FirstOrDefaultAsync();
        return r;
    }

    public async Task<bool> IsContractedBank(Guid id)
    {
        var res = await dbContext.Banks.AnyAsync(b => b.Id == id && b.Contracted);
        return res;
    }

    public async Task<BankAccount> ccc(BankAccount bank)
    {
        var bankAccount = await _db.BankAccounts.FirstOrDefaultAsync(x =>
            x.BankId == bank.BankId && x.Id == bank.Id
        );
        return bankAccount;
    }
}
