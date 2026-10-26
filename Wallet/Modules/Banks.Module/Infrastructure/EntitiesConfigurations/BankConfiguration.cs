using Banks.Contracts.Entities;
using Banks.Contrancts.Enums;
using Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banks.Infrastructure.EntitiesConfigurations;

public class BankConfiguration : IEntityTypeConfiguration<Bank>
{
    public void Configure(EntityTypeBuilder<Bank> builder)
    {
        builder.HasKey(bank => bank.Id);
        builder.Property(bank => bank.Name).IsRequired();

        builder
            .HasMany(bank => bank.BankAccounts)
            .WithOne(client => client.Bank)
            .HasForeignKey(client => client.BankId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(bank => bank.BankAccounts)
            .WithOne(account => account.Bank)
            .HasForeignKey(account => account.BankId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(bank => bank.Name).HasConversion<string>();

        builder.HasData([
            new Bank()
            {
                Id = Guid.Parse("c9337615-fb2b-4be1-9da0-c9704d546bf6"),
                Name = ContractedBanks.Acme,
                Contracted = true
            },
            new Bank()
            {
                Id = Guid.Parse("4b42ad75-6775-44af-931f-f5fb3f38b75a"),
                Name = ContractedBanks.PayTech,
                Contracted = true
            },
            new Bank()
            {
                Id = Guid.Parse("0eb2b1cc-6144-4bf9-a5f0-27b937b94e22"),
                Name = ContractedBanks.GothamPayments
            },
            new Bank()
            {
                Id = Guid.Parse("d872cb88-d531-4268-b2c0-b140d3cd8b6f"),
                Name = ContractedBanks.BanqueTech
            },
        ]);
    }
}
