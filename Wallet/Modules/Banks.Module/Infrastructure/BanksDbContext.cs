using Banks.Domain;
using Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banks.Infrastructure;

public class BanksDbContext : DbContext
{
    public BanksDbContext(DbContextOptions<BanksDbContext> options)
        : base(options) { }
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankAccountBalance> BankAccountsBalances => Set<BankAccountBalance>();
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<InboundWebhookMessage> InboundWebhookMessages => Set<InboundWebhookMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("banks");

        builder.ApplyConfigurationsFromAssembly(typeof(BanksDbContext).Assembly);
    }
}
