using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankSimulator.Domain;

public class TransactionMetadata
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey("Transaction")]
    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; }

    public required string Key { get; set; }
    public required string Value { get; set; }
}
