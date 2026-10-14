using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Finova.Modules.Banks.Application.Services;

public sealed class BankWebhooksServices(
    WebhookHeaderSettings WebhookSettings,
    BankTransactionWebhookValidationServices webhookValidationServices,
    ILogger<BankWebhooksServices> logger,
    IConfiguration configuration
)
{
    private readonly string SecretKey = configuration["Webhooks:BankSecret"];
    private Byte[] _secretKeyBytes;
    private byte[] _receivedSignatureBytes;
    private byte[] _receivedTimestampBytes;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (
            !webhookValidationServices.ValidateRequestHeaders(
                context,
                out byte[] receivedSignatureBytes,
                out byte[] receivedTimestampBytes
            )
        )
            return;

        _receivedSignatureBytes = receivedSignatureBytes;
        _receivedTimestampBytes = receivedTimestampBytes;

        if (!TryGetSecretKeyBytes(context))
            return;

        var isAuthorized = await AuthorizeWebhook(context);
        if (!isAuthorized)
        {
            context.Result = new UnauthorizedObjectResult(
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Invalid Webhook Key",
                    Detail = "Provide a valid webhook",
                }
            );
        }
    }

    private async Task<bool> AuthorizeWebhook(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        request.EnableBuffering();
        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        } 
        
        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, _secretKeyBytes);

        hmac.AppendData(_receivedTimestampBytes);
        hmac.AppendData(WebhookSettings.Encode("."));

        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await request.Body.ReadAsync(buffer)) > 0)
        {
            hmac.AppendData(buffer, 0, bytesRead);
        }
        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }
        byte[] expectedSignature = hmac.GetHashAndReset();

        var isSignaturesMatch = CryptographicOperations.FixedTimeEquals(
            expectedSignature,
            _receivedSignatureBytes
        );

        if (isSignaturesMatch)
            return true;

        return false;
    }

    public bool TryGetSecretKeyBytes(AuthorizationFilterContext context)
    {
        if (string.IsNullOrEmpty(SecretKey))
        {
            logger.LogError("Webhook's BankSecret key is empty or null");

            context.Result = new ObjectResult("Internal server error we're working on it")
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
            };
            throw new KeyNotFoundException("Bank secret Key not found");
        }
        _secretKeyBytes = WebhookSettings.Encode(SecretKey);
        return true;
    }
}
