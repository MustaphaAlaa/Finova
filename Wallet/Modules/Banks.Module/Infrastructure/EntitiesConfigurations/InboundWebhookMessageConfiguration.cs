using Banks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banks.Infrastructure.EntitiesConfigurations;

public class InboundWebhookMessageConfiguration : IEntityTypeConfiguration<InboundWebhookMessage>
{
    public void Configure(EntityTypeBuilder<InboundWebhookMessage> builder)
    {
        builder.HasKey(msg => msg.Id);
        builder.Property(x => x.Payload)
            .HasColumnType("jsonb");
        builder.HasIndex(msg => new { msg.ReceivedAtUtc, msg.Id })
            .HasFilter("\"Status\" = 0");
    }
}