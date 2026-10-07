using System.ComponentModel.DataAnnotations.Schema;

namespace BankSimulator.Domain;

public sealed class TransactionWebhookSubscription
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;

    [ForeignKey("Client")]
    public Guid ClientId { get; set; }
    public Client Client { get; set; }
    public DateTime CreatedOnUTC { get; set; }
}


