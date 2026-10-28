using System.Globalization;
using System.Text.Json;
using Transaction.Contracts.Entities;
using Transaction.Contracts.Interfaces;

namespace Transactions.Application.Services.BankTransactionParserFactory;

public class PayTechParser : ITransactionStringParser
{
    public BankTransaction Parse(string transactionString)
    {
        // Transaction Format: yyyyMMddHHmm,Amount#could be any string#Key/Value/Key/Value

        ArgumentException.ThrowIfNullOrWhiteSpace(transactionString);

        ReadOnlySpan<char> chars = transactionString.AsSpan();

        var firstSeparator = chars.IndexOf(',');
        var secondSeparator = chars.IndexOf('#');
        var thirdSeparator = chars.LastIndexOf('#');

        if (firstSeparator == -1 || secondSeparator == -1 || thirdSeparator == -1)
            throw new FormatException("Invalid Transaction Format");

        DateTime parsedDate = GetDate(chars, firstSeparator);

        decimal parsedAmount = GetAmount(chars, firstSeparator, secondSeparator);

        Dictionary<string, string> metadataDictionary = GetMetadateDictionary(
            chars,
            thirdSeparator
        );

        ReadOnlySpan<char> reference = chars[(secondSeparator + 1)..thirdSeparator];

        var transactionNotification = new BankTransaction
        {
            Amount = parsedAmount,
            Reference = reference.ToString(),
            Date = parsedDate,
            Metadata = JsonDocument.Parse(JsonSerializer.Serialize(metadataDictionary)),
        };
        return transactionNotification;
    }

    private DateTime GetDate(ReadOnlySpan<char> chars, int firstSeparator)
    {
        ReadOnlySpan<char> date = chars[..firstSeparator];

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

    private decimal GetAmount(ReadOnlySpan<char> chars, int firstSeparator, int secondSeparator)
    {
        ReadOnlySpan<char> amount = chars[(firstSeparator + 1)..secondSeparator];

        if (!decimal.TryParse(amount, out var parsedAmount))
            throw new FormatException("Cannot Parse the Amount");
        return parsedAmount;
    }

    private Dictionary<string, string> GetMetadateDictionary(
        ReadOnlySpan<char> chars,
        int thirdSeparator
    )
    {
        ReadOnlySpan<char> metadata = chars[(thirdSeparator + 1)..];

        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        var tokens = metadata.Split('/');

        string? key = null;

        foreach (var range in tokens)
        {
            ReadOnlySpan<char> token = metadata[range];

            if (key is null)
            {
                if (token.IsEmpty)
                    throw new FormatException("Metadata key cannot be empty.");

                key = token.ToString();
            }
            else
            {
                if (!result.TryAdd(key, token.ToString()))
                    throw new FormatException($"Duplicate metadata key: {key}");

                key = null;
            }
        }

        if (key is not null)
            throw new FormatException("Metadata contains a key without a value.");

        return result;
    }
}
