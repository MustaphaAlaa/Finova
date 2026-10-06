namespace BankSimulator.Domain;

public sealed class WebhookSubscription
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public DateTime CreatedOnUTC { get; set; }
}
