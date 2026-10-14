using Finova.Modules.Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace Finova.Modules.Banks.Infrastructure;

internal class BanksDbContext : DbContext
{
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<Currency> Currencies => Set<Currency>();

    // public DbSet<TransactionWebhookSubscription> TransactionWebhookSubscriptions => Set<TransactionWebhookSubscription>();
    // public DbSet<Transaction> Transactions => Set<Transaction>();
    // public DbSet<TransactionMetadata> TransactionsMetadata => Set<TransactionMetadata>();
    // public DbSet<WebhookOutboxMessage> WebhookOutboxMessages => Set<WebhookOutboxMessage>();

    public BanksDbContext(DbContextOptions<BanksDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("banks");

        builder.ApplyConfigurationsFromAssembly(typeof(BanksDbContext).Assembly);
    }
}
