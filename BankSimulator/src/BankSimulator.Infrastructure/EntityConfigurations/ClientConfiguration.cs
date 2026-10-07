using System.Text.Json;
using BankSimulator.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;

namespace BankSimulator.Infrastructure;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(client => client.Id);
        builder.Property(client => client.Name).IsRequired();
        builder.Property(client => client.Balance).HasPrecision(18, 2);

        builder
            .HasMany(client => client.BankAccounts)
            .WithOne(account => account.Client)
            .HasForeignKey(account => account.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        var clients = this.ClientsData();
        if (clients.Count > 0)
            builder.HasData(clients);
    }

    private IList<Client> ClientsData()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "Clients.json");

        using FileStream openStream = File.OpenRead(filePath);
        var clients = JsonSerializer.Deserialize<List<Client>>(openStream);
        return clients ?? new List<Client>();
    }
}
