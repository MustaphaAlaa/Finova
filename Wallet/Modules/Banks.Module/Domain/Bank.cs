namespace Finova.Modules.Banks.Domain.Entities;

public class Bank
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Contracted { get; set; }
    public List<BankAccount> BankAccounts { get; set; }
    public string TransactionSeparator { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}


// if bank exist and if we have the bank account and bank account is for one of our users
