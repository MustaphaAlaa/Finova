namespace BankSimulator.Domain;

public class BankAccount
{
    public Guid Id { get; set; }
    public int AccountNumber { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public Guid BankId { get; set; }
    public Bank Bank { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
