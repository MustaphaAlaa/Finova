using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Finova.Modules.Banks.Application.Services;

public sealed class BankTransactionWebhookServices(
    ILogger<BankTransactionWebhookServices> logger,
    IConfiguration configuration
)
{
    private const string SignatureHeader = "X-Webhook-Signature";
    private const string TimestampHeader = "X-Webhook-Timestamp";

    public async Task<bool> AuthorizeWebhook(
        ActionExecutingContext context,
        string webhookSignatureHeader
    )
    {
        if (!ValidateRequestHeaders(context))
            return false;

        var request = context.HttpContext.Request;
        var providedSignatureBytes = request.Headers[webhookSignatureHeader];

        using var reader = new StreamReader(request.Body, Encoding.UTF8);

        var rawBody = await reader.ReadToEndAsync();

        var expectedSignature = configuration["Webhooks:BankSecret"];
        var fr = "secret.time.body";
        if (string.IsNullOrEmpty(expectedSignature))
        {
            logger.LogError("Webhook's BankSecret key is empty or null");

            context.Result = new ObjectResult("Internal server error we're working on it")
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
            };
            return false;
        }
        var r = CryptographicOperations.FixedTimeEquals(
            Encoding.Unicode.GetBytes(expectedSignature),
            Encoding.Unicode.GetBytes(providedSignatureBytes!)
        );
        if (!r)
        {
            logger.LogError("Invalid Webhook secret key");
            context.Result = new UnauthorizedObjectResult("You're not authorized");
            return false;
        }
        return true;
    }

    private bool ValidateRequestHeaders(ActionExecutingContext context)
    {
        var headers = context.HttpContext.Request.Headers;

        bool checkSignatureHeader = CheckSignatureHeader(context, headers);

        if (!checkSignatureHeader)
            return false;

        bool validateTimestampHeader = ValidateTimestampHeader(context, headers);

        if (!validateTimestampHeader)
            return false;

        return true;
    }

    private static bool CheckSignatureHeader(
        ActionExecutingContext context,
        Microsoft.AspNetCore.Http.IHeaderDictionary headers
    )
    {
        if (
            !headers.TryGetValue(SignatureHeader, out var signature)
            || string.IsNullOrWhiteSpace(signature)
        )
        {
            context.Result = new BadRequestObjectResult(
                "Missing or empty X-Webhook-Signature header."
            );
            return false;
        }

        return true;
    }

    private bool ValidateTimestampHeader(
        ActionExecutingContext context,
        Microsoft.AspNetCore.Http.IHeaderDictionary headers
    )
    {
        if (
            !headers.TryGetValue(TimestampHeader, out var timestampHeaderValue)
            || !long.TryParse(
                timestampHeaderValue,
                CultureInfo.InvariantCulture,
                out var unixTimestampSeconds
            )
        )
        {
            context.Result = new BadRequestObjectResult(
                "Missing or invalid X-Webhook-Timestamp header."
            );
            return false;
        }
        else
        {
            var minimumAllowedTimestampTicks = DateTime.UtcNow.Ticks - TimeSpan.TicksPerMinute * 3;
            var requestTimestampTicks = DateTimeOffset
                .FromUnixTimeSeconds(unixTimestampSeconds)
                .Ticks;

            if (
                minimumAllowedTimestampTicks > requestTimestampTicks
                || requestTimestampTicks > DateTime.UtcNow.Ticks
            )
            {
                context.Result = new BadRequestObjectResult("Outdated Request Or Invalid Request");
                return false;
            }
        }

        return true;
    }
}
