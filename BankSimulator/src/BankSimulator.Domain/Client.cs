namespace BankSimulator.Domain;

public class Client
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string TransactionSeparator { get; set; } = string.Empty;

    public Guid BankId { get; set; }
    public Bank Bank { get; set; } = null!;
    public ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
}
