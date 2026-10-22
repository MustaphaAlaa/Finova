using Banks.Domain;
using Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banks.Infrastructure;

internal class BanksDbContext : DbContext
{
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<InboundWebhookMessage> InboundWebhookMessages => Set<InboundWebhookMessage>();

    public BanksDbContext(DbContextOptions<BanksDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("banks");

        builder.ApplyConfigurationsFromAssembly(typeof(BanksDbContext).Assembly);
    }
}
