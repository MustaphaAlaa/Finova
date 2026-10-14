using Finova.Modules.Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finova.Modules.Banks.Infrastructure;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder
            .HasOne(x => x.Bank)
            .WithMany(x => x.BankAccounts)
            .HasForeignKey(x => x.BankId)
            .IsRequired();

        builder
            .HasOne(x => x.Currency)
            .WithMany(x => x.BankAccounts)
            .HasForeignKey(x => x.CurrencyId)
            .IsRequired();

        builder.HasIndex(x => x.ApplicationUserId);

        builder.HasIndex(x => new
        {
            x.BankId,
            x.AccountNumber,
            x.CurrencyId,
        });
    }
}
