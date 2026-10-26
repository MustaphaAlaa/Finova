using Banks.Application.Interfaces.IRepositories;
using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
using Banks.Contrancts.Enums;
using Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Repositories;

namespace Banks.Infrastructure.Repositories;

internal sealed class BankRepository(BanksDbContext dbContext)
    : BaseRepository<Bank, BanksDbContext>(dbContext),
        IBankRepository
{
    public async Task<Bank> GetBankByName(ContractedBanks bank)
    {
        var res = await Db.Banks.SingleOrDefaultAsync(b => b.Name == bank);
        return res;
    }

    public async Task<bool> IsContractedBank(Guid id)
    {
        var res = await Db.Banks.AnyAsync(b => b.Id == id && b.Contracted);
        return res;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> UpdateMessageStatusAsync(
        InboundWebhookMessage message,
        InboundTransactionWebhookOutboxMessageStatus status,
        CancellationToken cancellationToken = default
    )
    {
        return await Db
            .InboundWebhookMessages.Where(msg => msg.Id == message.Id)
            .ExecuteUpdateAsync(
                (msg) =>
                {
                    msg.SetProperty(m => m.Status, status);
                },
                cancellationToken
            );
    }
}
