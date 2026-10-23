namespace Banks.Domain.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class BankAccount
{
    public Guid Id { get; set; }
    public required string AccountNumber { get; set; }

    [ForeignKey("ApplicationUser")]
    public required Guid ApplicationUserId { get; set; }

    [ForeignKey("Bank")]
    public required Guid BankId { get; set; }
    public Bank Bank { get; init; }  

    [ForeignKey("Currency")]
    public required Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
}
