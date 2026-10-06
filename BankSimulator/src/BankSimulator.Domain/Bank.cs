namespace BankSimulator.Domain;

public class Bank
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Client> Clients { get; set; } = new List<Client>();
    public ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
    public string TransactionSeparator { get; set; } = string.Empty;
}
