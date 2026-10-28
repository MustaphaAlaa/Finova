using Banks.Contracts.Entities;
using Transaction.Contracts.Entity;

namespace Banks.Domain.Entities;

public class Currency
{
    public Guid Id { get; set; }
    public required Currencies Code { get; set; }
    public required string Name { get; set; }
} 