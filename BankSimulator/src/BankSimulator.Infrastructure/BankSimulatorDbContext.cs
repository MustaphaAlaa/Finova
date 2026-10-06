using BankSimulator.Domain;
using Microsoft.EntityFrameworkCore;

namespace BankSimulator.Infrastructure;

public class BankSimulatorDbContext : DbContext
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<TransactionWebhookSubscription> WebhookSubscriptions => Set<TransactionWebhookSubscription>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionMetadata> TransactionsMetadata => Set<TransactionMetadata>();

    public BankSimulatorDbContext(DbContextOptions<BankSimulatorDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(BankSimulatorDbContext).Assembly);
        
    }
}
