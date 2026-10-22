using Banks.Domain.Entities;
using Finova.Shared.Infrastructure.Interfaces;

namespace Banks.Application.Interfaces.IRepositories;

public interface IBankRepository : IBaseRepository<Bank>
{
    Task<Bank> GetBankByName(Domain.Entities.ContractedBanks bank);
    Task<bool> IsContractedBank(Guid id);
}