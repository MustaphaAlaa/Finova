using Finova.Modules.Banks.Application;

namespace Finova.Modules.Banks.Builders;

internal class WebhookHeadersBuilder
{
    private byte[] _signatureBytes;
    private byte[] _timestampBytes;
    private byte[] _secretKeyBytes;

    public WebhookHeadersBuilder AddSignatureBytes(byte[] signatureBytes)
    {
        _signatureBytes = signatureBytes;
        return this;
    }

    public WebhookHeadersBuilder AddTimestampBytes(byte[] timestampBytes)
    {
        _timestampBytes = timestampBytes;
        return this;
    }

    public WebhookHeadersBuilder AddSecretKeyBytes(byte[] secretKey)
    {
        _secretKeyBytes = secretKey;
        return this;
    }

    public WebhookHeaders Build()
    {
        return new WebhookHeaders(_signatureBytes, _timestampBytes, _secretKeyBytes);
    }
}
