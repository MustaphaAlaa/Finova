namespace Finova.Modules.Banks.Application;

public class WebhookHeaders
{
    public byte[] SignatureBytes { get; set; }
    public byte[] TimestampBytes { get; set; }
    public byte[] SecretKeyBytes { get; set; }

    public WebhookHeaders(byte[] signatureBytes, byte[] timestampBytes, byte[] secretKeyBytes)
    {
        this.SignatureBytes = signatureBytes;
        this.TimestampBytes = timestampBytes;
        this.SecretKeyBytes = secretKeyBytes;
    }
}
