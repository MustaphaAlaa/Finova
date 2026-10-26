using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transactions.Contracts.Entities;

namespace Transactions.Infrastructe.EntitiesConfigurations;

public class BankTransactionStagingConfigurations : IEntityTypeConfiguration<BankTransactionStaging>
{
    public void Configure(EntityTypeBuilder<BankTransactionStaging> builder)
    {

        builder.HasKey(x => x.Id);
        builder.ToTable("bank_transaction_stage");
        builder.HasIndex(bankTransactionStaging => bankTransactionStaging.StagedAtUtc).HasFilter("\"InsertedAtUtc\" IS NULL");
   
        builder.HasIndex(x => new { x.TransactionId, x.Reference }).IsUnique();
    }


}