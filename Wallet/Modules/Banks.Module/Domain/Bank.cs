namespace Banks.Domain.Entities;

public class Bank
{
    public Guid Id { get; set; }
    public ContractedBanks Name { get; set; }
    public bool Contracted { get; set; }
    public List<BankAccount> BankAccounts { get; set; }
    public List<InboundWebhookMessage> InboundWebhookMessages { get; set; }
}