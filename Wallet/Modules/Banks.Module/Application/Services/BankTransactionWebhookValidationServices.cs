using System.Globalization;
using System.Net;
using Banks.Application.Interfaces;
using Banks.Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Shared.Services.Interfaces;

namespace Banks.Application.Services;

internal sealed class BankTransactionWebhookValidationServices(
    WebhookHeaderSettings webhookSettings,
    IRequestUtilities requestUtilities,
    ILogger<BankWebhooksServices> logger
) : IBankTransactionWebhookValidationServices
{
    private string _routeParameter;

    private WebhookHeadersBuilder _webhookHeadersBuilder = new();

    public WebhookHeaders GetWebhookHeadersBytes()
    {
        return _webhookHeadersBuilder.Build();
    }

    public bool ValidateRequestHeaders(AuthorizationFilterContext context)
    {
        var headers = context.HttpContext.Request.Headers;

        _routeParameter = requestUtilities.GetRouteParameterValue(
            context,
            context.Result,
            "bankCode"
        );

        bool checkSignatureHeader = TryValidateSignatureHeader(context, headers);

        if (!checkSignatureHeader)
            return false;

        bool validateTimestampHeader = TryValidateTimestampHeader(context, headers);

        if (!validateTimestampHeader)
            return false;

        if (!TryGetSecretKeyBytes(context))
            return false;

        return true;
    }

    private bool TryValidateSignatureHeader(
        AuthorizationFilterContext context,
        IHeaderDictionary headers
    )
    {
        var signatureHeader = webhookSettings.GetSignatureHeader(_routeParameter);

        if (
            !headers.TryGetValue(signatureHeader, out var signature)
            || string.IsNullOrWhiteSpace(signature)
        )
        {
            context.Result = new BadRequestObjectResult(
                "Missing or empty X-Webhook-Signature header."
            );
            return false;
        }

        byte[] receivedSignatureBytes;
        try
        {
            receivedSignatureBytes = Convert.FromHexString(signature!);
        }
        catch (FormatException ex)
        {
            logger.LogError($"Invalid signature format, it's not HexString. {ex.Message}", ex);
            context.Result = new BadRequestObjectResult("Provide valid webhook signature.")
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
            };
            return false;
        }
        _webhookHeadersBuilder.AddSignatureBytes(receivedSignatureBytes);
        return true;
    }

    private bool TryValidateTimestampHeader(
        AuthorizationFilterContext context,
        IHeaderDictionary headers
    )
    {
        try
        {
            var timestampKey = webhookSettings.GetTimestampHeader((string)_routeParameter);

            if (
                !headers.TryGetValue(timestampKey, out var timestampHeaderValue)
                || !int.TryParse(
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
                try
                {
                    var minimumAllowedTimestampTicks =
                        DateTime.UtcNow.Ticks - TimeSpan.TicksPerMinute * 60 * 124;
                    var requestTimestampTicks = DateTimeOffset
                        .FromUnixTimeSeconds(unixTimestampSeconds)
                        .Ticks;

                    if (
                        minimumAllowedTimestampTicks > requestTimestampTicks
                        || requestTimestampTicks > DateTime.UtcNow.Ticks
                    )
                    {
                        context.Result = new BadRequestObjectResult(
                            "Outdated Request Or Invalid Request"
                        );
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    return false;
                }
            }

            _webhookHeadersBuilder.AddTimestampBytes(webhookSettings.Encode(timestampHeaderValue!));

            return true;
        }
        catch
        {
            throw;
        }
    }

    private bool TryGetSecretKeyBytes(AuthorizationFilterContext context)
    {
        try
        {
            var secretKey = webhookSettings.GetWebhookSecretKey((string)_routeParameter);
            var secretKeyBytes = webhookSettings.Encode(secretKey);

            _webhookHeadersBuilder.AddSecretKeyBytes(secretKeyBytes);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Webhook's BankSecret key is empty or null, {ex.Message}", ex);

            context.Result = new ObjectResult("Internal server error we're working on it")
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
            };
        }
        return false;
    }
}
