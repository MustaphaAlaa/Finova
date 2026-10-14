using System.Text;
using Microsoft.Extensions.Configuration;

namespace Finova.Modules.Banks.Application.Services;

public class WebhookHeaderSettings(IConfiguration configuration)
{
    public byte[] Encode(string value)
    {
        if (value is null)
        {
            throw new NullReferenceException("Cannot encode null value.");
        }
        return Encoding.UTF8.GetBytes(value);
    }

    public string GetSignatureHeader(string bankCode)
    {
        var signature = configuration[$"WebhooksHeaders:{bankCode}:Signature"];
        if (signature is null)
            throw new InvalidOperationException(
                $"Missing signature header configuration for bank '{bankCode}'."
            );
        return signature;
    }

    public string GetWebhookSecretKey(string bankCode)
    {
        var signature = configuration[$"WebhooksHeaders:{bankCode}:SecretKey"];
        if (signature is null)
        {
            throw new InvalidOperationException($"Missing the secret for '{bankCode}'s webhook'.");
        }
        return signature;
    }

    public string GetTimestampHeader(string bankCode)
    {
        return configuration[$"WebhooksHeaders:{bankCode}:Timestamp"]
            ?? throw new InvalidOperationException(
                $"Missing timestamp header configuration for bank '{bankCode}'."
            );
    }
}
