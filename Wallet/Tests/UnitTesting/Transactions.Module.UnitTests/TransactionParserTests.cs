using FluentAssertions;
using Transactions.Application.Services.BankTransactionParserFactory;

namespace Transactions.Module.UnitTests;

public class TransactionParserTests
{
    private readonly AcmeParser _acme = new();
    private readonly PayTechParser _payTech = new();

    [Fact]
    public void AcmeParser_WithValidTransaction_ReturnsRoundedAmountReferenceAndUtcDate()
    {
        var result = _acme.Parse("12.345//invoice-42//202610311425");

        result.Amount.Should().Be(12.34m);
        result.Reference.Should().Be("invoice-42");
        result.Date.Should().Be(new DateTime(2026, 10, 31, 14, 25, 0, DateTimeKind.Utc));
        result.Metadata.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AcmeParser_WithNullOrWhitespaceInput_ThrowsArgumentException(string? transaction)
    {
        var act = () => _acme.Parse(transaction!);

        act.Should().Throw<ArgumentException>();
     }

    [Theory]
    [InlineData("12.34")]
    [InlineData("12.34//ref")]
    public void AcmeParser_WhenSeparatorsAreMissing_ThrowsFormatException(string transaction)
    {
        var act = () => _acme.Parse(transaction);

        act.Should().Throw<FormatException>().WithMessage("Invalid transaction format.");
    }

    [Fact]
    public void AcmeParser_WhenAmountIsInvalid_ThrowsFormatException()
    {
        var act = () => _acme.Parse("many//ref//202610311425");

        act.Should().Throw<FormatException>().WithMessage("Cannot Parse the Amount");
    }

    [Fact]
    public void AcmeParser_WhenDateDoesNotMatchExpectedFormat_ThrowsFormatException()
    {
        var act = () => _acme.Parse("12.34//ref//31-10-2026");

        act.Should().Throw<FormatException>().WithMessage("Cannot Parse the Date");
    }

    [Fact]
    public void PayTechParser_WithValidTransaction_ReturnsFieldsAndMetadata()
    {
        var result = _payTech.Parse("202610311425,12.50#invoice-42#terminal/pos/region/eg");

        result.Amount.Should().Be(12.50m);
        result.Reference.Should().Be("invoice-42");
        result.Date.Should().Be(new DateTime(2026, 10, 31, 14, 25, 0, DateTimeKind.Utc));
        result.Metadata.Should().NotBeNull();
        result.Metadata!.RootElement.GetProperty("terminal").GetString().Should().Be("pos");
        result.Metadata.RootElement.GetProperty("region").GetString().Should().Be("eg");
        // Catches dropped transaction fields or incorrect key/value metadata pairing.
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void PayTechParser_WithNullOrWhitespaceInput_ThrowsArgumentException(string? transaction)
    {
        var act = () => _payTech.Parse(transaction!);

        act.Should().Throw<ArgumentException>();
        // Catches blank PayTech payload entries escaping parser validation.
    }

    [Fact]
    public void PayTechParser_WhenRequiredSeparatorsAreMissing_ThrowsFormatException()
    {
        var act = () => _payTech.Parse("202610311425,12.50");

        act.Should().Throw<FormatException>().WithMessage("Invalid Transaction Format");
        // Catches incomplete PayTech strings being treated as valid transactions.
    }

    [Fact]
    public void PayTechParser_WhenDateIsInvalid_ThrowsFormatException()
    {
        var act = () => _payTech.Parse("31-10-2026,12.50#ref#key/value");

        act.Should().Throw<FormatException>().WithMessage("Cannot Parse the Date");
        // Catches malformed timestamps being accepted with a default date.
    }

    [Fact]
    public void PayTechParser_WhenAmountIsInvalid_ThrowsFormatException()
    {
        var act = () => _payTech.Parse("202610311425,abc#ref#key/value");

        act.Should().Throw<FormatException>().WithMessage("Cannot Parse the Amount");
        // Catches invalid monetary values being accepted as a transaction amount.
    }

    [Fact]
    public void PayTechParser_WhenMetadataKeyIsEmpty_ThrowsFormatException()
    {
        var act = () => _payTech.Parse("202610311425,12.50#ref#/value");

        act.Should().Throw<FormatException>().WithMessage("Metadata key cannot be empty.");
        // Catches metadata with an empty key being silently persisted.
    }

    [Fact]
    public void PayTechParser_WhenMetadataContainsDuplicateKeys_ThrowsFormatException()
    {
        var act = () => _payTech.Parse("202610311425,12.50#ref#key/one/key/two");

        act.Should().Throw<FormatException>().WithMessage("Duplicate metadata key: key");
        // Catches duplicate metadata overwriting an earlier value unpredictably.
    }

    [Fact]
    public void PayTechParser_WhenMetadataHasKeyWithoutValue_ThrowsFormatException()
    {
        var act = () => _payTech.Parse("202610311425,12.50#ref#key");

        act.Should().Throw<FormatException>().WithMessage("Metadata contains a key without a value.");
        // Catches truncated metadata records being accepted as complete pairs.
    }
}
