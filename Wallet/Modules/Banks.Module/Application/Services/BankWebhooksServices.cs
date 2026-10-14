using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using Finova.Modules.Banks.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Finova.Modules.Banks.Application.Services;

internal sealed class BankWebhooksServices(
    WebhookHeaderSettings WebhookSettings,
    IBankTransactionWebhookValidationServices webhookValidationServices,
    ILogger<BankWebhooksServices> logger
) : IBankWebhooksServices
{
    WebhookHeaders _webhookHeaders;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var requestId = context.HttpContext.TraceIdentifier;

        logger.LogDebug("Starting bank webhook authorization. RequestId: {RequestId}", requestId);

        if (!webhookValidationServices.ValidateRequestHeaders(context))
        {
            logger.LogWarning(
                "Bank webhook authorization rejected because the required "
                    + "headers are invalid or missing. RequestId: {RequestId}",
                requestId
            );
            return;
        }

        _webhookHeaders = webhookValidationServices.GetWebhookHeadersBytes();

        var isAuthorized = await AuthorizeWebhook(context);
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

    private async Task<bool> AuthorizeWebhook(AuthorizationFilterContext context)
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
                _webhookHeaders.SecretKeyBytes
            );

            hmac.AppendData(_webhookHeaders.TimestampBytes);
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
                _webhookHeaders.SignatureBytes
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
