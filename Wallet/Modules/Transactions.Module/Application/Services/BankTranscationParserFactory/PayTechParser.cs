using System.Globalization;
using System.Text.Json;
using Transaction.Contracts.IFactory;
using Transactions.Domain;

namespace Transactions.Application.Services.BankTranscationParserFactory;

public class PayTechParser : ITransactionStringParser
{
    public BankTransaction Parse(string transactionString)
    {
        // Transaction Format: yyyyMMddHHmm,Amount#could be any string#Key/Value/Key/Value

        ArgumentException.ThrowIfNullOrWhiteSpace(transactionString);

        ReadOnlySpan<char> chars = transactionString.AsSpan();

        var firstSepartor = chars.IndexOf(',');
        var secondSepartor = chars.IndexOf('#');
        var thirdSepartor = chars.LastIndexOf('#');

        if (firstSepartor == -1 || secondSepartor == -1 || thirdSepartor == -1)
            throw new FormatException("Invalid Transaction Format");

        DateTime parsedDate = GetDate(chars, firstSepartor);

        decimal parsedAmount = GetAmount(chars, firstSepartor, secondSepartor);

        Dictionary<string, string> metadataDictionary = GetMetadateDictionary(chars, thirdSepartor);

        ReadOnlySpan<char> reference = chars[(secondSepartor + 1)..thirdSepartor];

        var transactionNotification = new BankTransaction
        {
            Amount = parsedAmount,
            Reference = reference.ToString(),
            Date = parsedDate,
            Paylod = JsonSerializer.Serialize(metadataDictionary)
        };
        return transactionNotification;
    }

    private DateTime GetDate(ReadOnlySpan<char> chars, int firstSepartor)
    {
        ReadOnlySpan<char> date = chars[..firstSepartor];

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

    private decimal GetAmount(ReadOnlySpan<char> chars, int firstSepartor, int secondSepartor)
    {
        ReadOnlySpan<char> amount = chars[(firstSepartor + 1)..secondSepartor];

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