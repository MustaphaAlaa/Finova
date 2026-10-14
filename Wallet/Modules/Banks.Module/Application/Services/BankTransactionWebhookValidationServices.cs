using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Shared.Services.Interfaces;

namespace Finova.Modules.Banks.Application.Services;

public sealed class BankTransactionWebhookValidationServices(
    WebhookHeaderSettings webhookSettings,
    IRequestUtilities requestUtilities,
    ILogger<BankWebhooksServices> logger
)
{
    private byte[] _receivedSignatureBytes;
    private byte[] _receivedTimestampBytes;
    private string _routeParameter;

    public bool ValidateRequestHeaders(
        AuthorizationFilterContext context,
        out byte[] receivedSignatureBytes,
        out byte[] receivedTimestampBytes
    )
    {
        receivedSignatureBytes = new byte[] { };
        receivedTimestampBytes = new byte[] { };

        var headers = context.HttpContext.Request.Headers;

        _routeParameter = requestUtilities.GetRouteParameterValue(context, "bankCode");

        bool checkSignatureHeader = TryValidateSignatureHeader(context, headers);

        if (!checkSignatureHeader)
            return false;

        bool validateTimestampHeader = TryValidateTimestampHeader(context, headers);
        receivedTimestampBytes = null;

        if (!validateTimestampHeader)
            return false;

        receivedSignatureBytes = _receivedSignatureBytes;
        receivedTimestampBytes = _receivedTimestampBytes;

        return true;
    }

    private bool TryValidateSignatureHeader(
        AuthorizationFilterContext context,
        IHeaderDictionary headers
    )
    {
        // api/bank-webhook/co/trans
        // var requestPath = context.HttpContext.Request.Path.ToString().Split('/');
        // var bnkUrl = requestPath[3];

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

        _receivedSignatureBytes = receivedSignatureBytes;
        return true;
    }

    private bool TryValidateTimestampHeader(
        AuthorizationFilterContext context,
        IHeaderDictionary headers
    )
    {
        try
        {
            // var requestPath = context.HttpContext.Request.Path.ToString().Split('/');
            // var bnkUrl = requestPath[3];

            // if (
            //     !context.RouteData.Values.TryGetValue("bankCode", out var routeValue)
            //     || routeValue is not string bankCode
            //     || string.IsNullOrWhiteSpace(bankCode)
            // )
            // {
            //     context.Result = new BadRequestObjectResult("Missing or invalid bank code.");
            //     return false;
            // }

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
                        DateTime.UtcNow.Ticks - TimeSpan.TicksPerMinute * 60 * 24;
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

            _receivedTimestampBytes = webhookSettings.Encode(timestampHeaderValue!);
            return true;
        }
        catch
        {
            throw;
        }
    }
}
