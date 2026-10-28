namespace Transaction.Contracts.Entities;

public class TransactionsRequest
{
    public Guid TransactionId { get; set; }
    public Guid WebhookDeliveryId { get; set; }
    public Guid Source { get; set; }
    public Guid Destination { get; set; }
    public string Currency { get; set; }
    public string Transaction { get; set; }

    public override string ToString()
    {
        return $"TransactionsRequest: {{ TransactionId = {TransactionId}, Source = {Source}, Destination = {Destination}, Currency = {Currency}, Transaction = {Transaction} }}";
    }
}

