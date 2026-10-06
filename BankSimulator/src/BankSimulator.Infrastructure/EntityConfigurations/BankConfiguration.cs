using BankSimulator.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankSimulator.Infrastructure;

public class BankConfiguration : IEntityTypeConfiguration<Bank>
{
    public void Configure(EntityTypeBuilder<Bank> builder)
    {
        builder.HasKey(bank => bank.Id);
        builder.Property(bank => bank.Name).IsRequired();
        builder.Property(bank => bank.TransactionSeparator).IsRequired();

        builder
            .HasMany(bank => bank.Clients)
            .WithOne(client => client.Bank)
            .HasForeignKey(client => client.BankId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(bank => bank.BankAccounts)
            .WithOne(account => account.Bank)
            .HasForeignKey(account => account.BankId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData([
            new Bank()
            {
                Id = Guid.Parse("5a79725d-e529-440f-b4cb-b30863e026e3"),
                Name = "Acme",
                TransactionSeparator = "//",
            },
            new Bank()
            {
                Id = Guid.Parse("6075887f-5029-4a05-b106-5ce30f1268c4"),
                Name = "PayTech",
                TransactionSeparator = "#",
            },
        ]);
    }
}
