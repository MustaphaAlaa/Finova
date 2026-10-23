using Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banks.Infrastructure.EntitiesConfigurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder
            .HasOne(x => x.Bank)
            .WithMany(x => x.BankAccounts)
            .HasForeignKey(x => x.BankId)
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