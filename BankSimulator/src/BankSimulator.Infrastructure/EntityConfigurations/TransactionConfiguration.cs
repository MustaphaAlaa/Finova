using BankSimulator.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankSimulator.Infrastructure;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Amount).HasPrecision(18, 2);
        builder.Property(transaction => transaction.Reference).IsRequired();
    }
}

public class TransactionMetaDataConfiguration : IEntityTypeConfiguration<TransactionMetadata>
{
    public void Configure(EntityTypeBuilder<TransactionMetadata> builder)
    {
        builder.HasKey(tm => tm.Id);

        builder.Property(tm => tm.TransactionId).IsRequired();
        builder.Property(tm => tm.Key).IsRequired();
        builder.Property(tm => tm.Value).IsRequired();

        builder
            .HasOne(tm => tm.Transaction)
            .WithMany(transaction => transaction.TransactionMetadata);
    }
}
