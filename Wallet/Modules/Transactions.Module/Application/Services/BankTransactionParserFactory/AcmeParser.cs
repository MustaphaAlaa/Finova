using System.Globalization;
using Transactions.Contracts.Entities;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class AcmeParser : ITransactionStringParser
{
    public BankTransaction Parse(string transactionString)
    {
        // Transaction Format:  Amount (two decimals)//reference, and it could be any string//date(yyyyMMddHHmm)

        ArgumentException.ThrowIfNullOrWhiteSpace(transactionString);

        ReadOnlySpan<char> chars = transactionString.AsSpan();

        var firstSeparator = chars.IndexOf("//");
        var secondSeparator = chars.LastIndexOf("//");

        if (firstSeparator <= 0 || secondSeparator <= firstSeparator)
        {
            throw new FormatException("Invalid transaction format.");
        }

        decimal parsedAmount = GetAmount(chars, firstSeparator);

        DateTime parsedDate = GetDate(chars, secondSeparator);

        string reference = chars[(firstSeparator + 2)..secondSeparator].ToString();
        var transactionNotification = new BankTransaction
        {
            Amount = parsedAmount,
            Reference = reference,
            Date = parsedDate,
            Metadata = null
        };
        return transactionNotification;
    }

    private DateTime GetDate(ReadOnlySpan<char> chars, int separtor)
    {
        ReadOnlySpan<char> date = chars[(separtor + 2)..];

        if (
            !DateTime.TryParseExact(
                date,
                "yyyyMMddHHmm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedDate
            )
        )
            throw new FormatException("Cannot Parse the Date");
        return parsedDate;
    }

    private decimal GetAmount(ReadOnlySpan<char> chars, int separator)
    {
        ReadOnlySpan<char> amount = chars[..separator];

        if (!decimal.TryParse(amount, out var parsedAmount))
            throw new FormatException("Cannot Parse the Amount");
        return Math.Round(parsedAmount, 2);
    }
}
