using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transaction.Contracts.Entities;

namespace Transaction.Infrastructure.EntitiesConfiguration;

public class BankTransactionConfigurations : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TransactionId, x.Reference }).IsUnique();
    } 
}
