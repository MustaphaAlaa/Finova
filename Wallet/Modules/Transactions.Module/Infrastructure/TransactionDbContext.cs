using Microsoft.EntityFrameworkCore;
using Transactions.Contracts.Entities;

public class TransactionsDbContext(DbContextOptions<TransactionsDbContext> options)
    : DbContext(options)
{
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();

    public DbSet<TransacitonProcessingRecord> TransacitonProcessingRecords =>
        Set<TransacitonProcessingRecord>();
    public DbSet<MessageProcessingRecord> MessageProcessingRecords =>
        Set<MessageProcessingRecord>();
    public DbSet<BankTransactionStaging> BankTransactionStagings => Set<BankTransactionStaging>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("transactions");
        
        builder.Entity<TransacitonProcessingRecord>()
        .HasKey(x=>x.Id);
        builder.Entity<TransacitonProcessingRecord>()
        .HasIndex(x => x.TransactionId)
        .IsUnique();
         
        builder.Entity<MessageProcessingRecord>()
  .HasKey(x=>x.Id);
         
        builder.Entity<MessageProcessingRecord>()
        .HasIndex(x => x.MessageId)
        .IsUnique();

        builder.ApplyConfigurationsFromAssembly(typeof(TransactionsDbContext).Assembly);
    }
}
