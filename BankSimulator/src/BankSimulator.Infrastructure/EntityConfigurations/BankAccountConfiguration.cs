using System.Text.Json;
using BankSimulator.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankSimulator.Infrastructure;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.HasKey(account => account.Id);
        builder.HasIndex(account => new { account.BankId, account.AccountNumber }).IsUnique();

        builder
            .HasMany(account => account.Transactions)
            .WithOne(transaction => transaction.BankAccount)
            .HasForeignKey(transaction => transaction.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        var accounts = this.BankAccountData();

        if (accounts.Count > 1)
            builder.HasData(accounts);
    }

    private List<BankAccount> BankAccountData()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "BankAccounts.json");

        using FileStream openStream = File.OpenRead(filePath);
        var accounts = JsonSerializer.Deserialize<List<BankAccount>>(openStream);
        return accounts ?? new List<BankAccount>();
    }
}
