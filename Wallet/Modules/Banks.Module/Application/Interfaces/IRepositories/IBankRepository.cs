using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
using Banks.Contrancts.Enums;
using Banks.Domain.Entities;
using Finova.Shared.Infrastructure.Interfaces;

namespace Banks.Application.Interfaces.IRepositories;

public interface IBankRepository : IBaseRepository<Bank>
{
    Task<Bank> GetBankByName(ContractedBanks bank);
    Task<bool> IsContractedBank(Guid id);
    Task<int> UpdateMessageStatusAsync(
        InboundWebhookMessage message,
        InboundTransactionWebhookOutboxMessageStatus status,
        CancellationToken cancellationToken = default
    );

    Task<int> SaveChangesAsync(CancellationToken cancelationToken = default);
}
