namespace Banks.Domain.Entities;

public class Bank
{
    public Guid Id { get; set; }
    public ContractedBanks Name { get; set; }
    public bool Contracted { get; set; }
    public List<BankAccount> BankAccounts { get; set; }
}


// if bank exist and if we have the bank account and bank account is for one of our users