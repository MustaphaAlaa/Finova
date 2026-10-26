using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transactions.Contracts.Entities;

namespace Transactions.Infrastructe.EntitiesConfigurations;

public class BankTransactionConfigurations : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TransactionId, x.Reference }).IsUnique();
    } 
}
