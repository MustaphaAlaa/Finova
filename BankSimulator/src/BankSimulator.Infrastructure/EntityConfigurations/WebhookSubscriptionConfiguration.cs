using BankSimulator.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankSimulator.Infrastructure;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<TransactionWebhookSubscription>
{
    public void Configure(EntityTypeBuilder<TransactionWebhookSubscription> builder)
    {
        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.EventType).IsRequired();
        builder.Property(subscription => subscription.WebhookUrl).IsRequired();
    }
}
