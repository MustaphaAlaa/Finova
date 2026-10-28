using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
using Banks.Domain.Entities;
using Shared.Infrastructure.Interfacse.IRepositrories;

namespace Banks.Application.Interfaces.IRepositories;

public interface IBankRepository : IBaseRepository<Bank>
{
    Task<Bank> GetBankByNameAsync(ContractedBanks bank, CancellationToken cancellationToken = default);
    Task<bool> IsContractedBank(Guid id, CancellationToken cancellationToken = default);

    Task<int> UpdateMessageStatusAsync(
        InboundWebhookMessage message,
        InboundTransactionWebhookOutboxMessageStatus status,
        CancellationToken cancellationToken = default
    );

    Task<int> SaveChangesAsync(CancellationToken cancelationToken = default);
}