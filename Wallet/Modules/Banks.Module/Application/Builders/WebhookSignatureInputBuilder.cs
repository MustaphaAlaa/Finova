using Banks.Application.Dtos;

namespace Banks.Application.Builders;

/// <summary>
/// Builds the signature validation inputs from the webhook's secret key,
/// timestamp, and received signature bytes.
/// </summary>
internal class WebhookSignatureInputBuilder
{
    private byte[] _signatureBytes;
    private byte[] _timestampBytes;
    private byte[] _secretKeyBytes;

    public WebhookSignatureInputBuilder SetSignatureBytes(byte[] signatureBytes)
    {
        _signatureBytes = signatureBytes;
        return this;
    }

    public WebhookSignatureInputBuilder SetTimestampBytes(byte[] timestampBytes)
    {
        _timestampBytes = timestampBytes;
        return this;
    }

    public WebhookSignatureInputBuilder SetSecretKeyBytes(byte[] secretKey)
    {
        _secretKeyBytes = secretKey;
        return this;
    }

    public WebhookSignatureInputs Build()
    {
        return new WebhookSignatureInputs(_signatureBytes, _timestampBytes, _secretKeyBytes);
    }
}
