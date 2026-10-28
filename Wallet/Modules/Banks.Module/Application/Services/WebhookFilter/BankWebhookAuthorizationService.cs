using System.Security.Cryptography;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Banks.Application.Services.WebhookFilter;

internal sealed class BankWebhookAuthorizationService(
    WebhookAuthenticationConfiguration webhookAuthenticationSettings,
    IWebhookRequestAuthenticator webhookRequestAuthenticator,
    ILogger<BankWebhookAuthorizationService> logger
) : IBankWebhookAuthorizationService
{
    WebhookSignatureInputs _webhookSignatureInputs;

    public async Task AuthorizeWebhookRequestAsync(AuthorizationFilterContext context)
    {
        var requestId = context.HttpContext.TraceIdentifier;

        logger.LogDebug("Starting bank webhook authorization. RequestId: {RequestId}", requestId);

        if (!webhookRequestAuthenticator.TryPrepareWebhookSignatureValidation(context))
        {
            logger.LogWarning(
                "Bank webhook authorization rejected because the required "
                + "headers are invalid or missing. RequestId: {RequestId}",
                requestId
            );


            var errorMessage = context.Result switch
            {
                ObjectResult result => result.Value?.ToString(),
                _ => context.Result?.ToString()
            };

            logger.LogError(
                "Webhook validation failed. StatusCode: {StatusCode}, Error: {Error}, RequestId: {RequestId}",
                (context.Result as ObjectResult)?.StatusCode,
                errorMessage,
                requestId
            );

            return;
        }

        _webhookSignatureInputs = webhookRequestAuthenticator.BuildWebhookSignatureValidationInputs();

        var isAuthorized = await IsWebhookSignatureValidAsync(context);
        if (!isAuthorized)
        {
            logger.LogWarning(
                "Bank webhook authorization failed due to an invalid "
                + "signature. RequestId: {RequestId}",
                requestId
            );
            context.Result = new UnauthorizedObjectResult(
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Invalid Webhook Key",
                    Detail = "Provide a valid webhook",
                }
            );
        }

        logger.LogDebug("Bank webhook authorization succeeded. RequestId: {RequestId}", requestId);
    }

    private async Task<bool> IsWebhookSignatureValidAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        request.EnableBuffering();
        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        try
        {
            using var hmac = IncrementalHash.CreateHMAC(
                HashAlgorithmName.SHA256,
                _webhookSignatureInputs.SecretKeyBytes
            );

            hmac.AppendData(_webhookSignatureInputs.TimestampBytes);
            hmac.AppendData(webhookAuthenticationSettings.EncodeUtf8("."));

            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await request.Body.ReadAsync(buffer, context.HttpContext.RequestAborted)) > 0)
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
                _webhookSignatureInputs.SignatureBytes
            );

            if (isSignaturesMatch)
                return true;

            return false;
        }
        finally
        {
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }
        }
    }
}