namespace Banks.Application.Dtos;

public class WebhookSignatureInputs(byte[] signatureBytes, byte[] timestampBytes, byte[] secretKeyBytes)
{
    public byte[] SignatureBytes { get; set; } = signatureBytes;
    public byte[] TimestampBytes { get; set; } = timestampBytes;
    public byte[] SecretKeyBytes { get; set; } = secretKeyBytes;
}
