using System.Text;
using Microsoft.Extensions.Configuration;
using Shared.Exceptions;

namespace Banks.Application.Services.WebhookFilter;

/// <summary>
/// Reads bank-specific webhook configuration, including signature and timestamp
/// header names and the secret key used for HMAC signature verification.
/// </summary> 
public class WebhookAuthenticationConfiguration(
    IConfiguration configuration)
{
    public byte[] EncodeUtf8(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Value cannot be null or empty or whitespace.",
                nameof(value));
        }

        return Encoding.UTF8.GetBytes(value);
    }

    public string GetSignatureHeaderName(string bankCode)
    {
        return GetRequiredWebhookConfigurationValue(
            bankCode,
            "Signature",
            $"Missing signature header configuration for bank '{bankCode}'.");
    }

    public string GetTimestampHeaderName(string bankCode)
    {
        return GetRequiredWebhookConfigurationValue(
            bankCode,
            "Timestamp",
            $"Missing timestamp header configuration for bank '{bankCode}'.");
    }

    public string GetWebhookSecretKey(string bankCode)
    {
        return GetRequiredWebhookConfigurationValue(
            bankCode,
            "SecretKey",
            $"Missing secret key configuration for bank '{bankCode}'.");
    }

    private string GetRequiredWebhookConfigurationValue(
        string bankCode,
        string key,
        string exceptionMessage)
    {
        var value = configuration[$"WebhooksHeaders:{bankCode}:{key}"];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InternalConfigurationException(exceptionMessage);
        }

        return value;
    }
}
