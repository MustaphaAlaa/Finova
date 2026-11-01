using System;
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

        builder.HasData(
            new BankAccount
            {
                Id = Guid.Parse("a68e3e38-0de1-4ad0-92c7-21b783702f81"),
                AccountNumber = "TEST-ACME-0001",
                ApplicationUserId = Guid.Parse("f108f490-6007-49bf-8f5c-c2fd24d9d909"),
                BankId = Guid.Parse("c9337615-fb2b-4be1-9da0-c9704d546bf6"),
                CurrencyId = Guid.Parse("00596123-1c69-4603-ac90-4bbe433621e9"),
            },
            new BankAccount
            {
                Id = Guid.Parse("be906bfd-7598-41dd-85c1-244b62fb2a36"),
                AccountNumber = "TEST-PAYTECH-0001",
                ApplicationUserId = Guid.Parse("f108f490-6007-49bf-8f5c-c2fd24d9d909"),
                BankId = Guid.Parse("4b42ad75-6775-44af-931f-f5fb3f38b75a"),
                CurrencyId = Guid.Parse("00596123-1c69-4603-ac90-4bbe433621e9"),
            });
    }
}