using Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banks.Infrastructure.EntitiesConfigurations;

public sealed class BankAccountBalanceConfiguration : IEntityTypeConfiguration<BankAccountBalance>
{
    public void Configure(EntityTypeBuilder<BankAccountBalance> builder)
    {
        builder.HasData(
            new BankAccountBalance
            {
                Id = Guid.Parse("635b69d6-d1f6-4b9a-93e6-06e3a8a40577"),
                BankAccountId = Guid.Parse("a68e3e38-0de1-4ad0-92c7-21b783702f81"),
                Balance = 25000.00m,
                LastUpdate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new BankAccountBalance
            {
                Id = Guid.Parse("4f3d9d40-43df-4d81-88f9-b293f7b85c22"),
                BankAccountId = Guid.Parse("be906bfd-7598-41dd-85c1-244b62fb2a36"),
                Balance = 12000.00m,
                LastUpdate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
    }
}
