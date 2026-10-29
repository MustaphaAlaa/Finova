using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using Banks.Application.Builders;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Interfaces.IServices;

[assembly: InternalsVisibleTo("Banks.Module.UnitTests")]
namespace Banks.Application.Services.WebhookFilter;

internal sealed class WebhookRequestAuthenticator(
    WebhookAuthenticationConfiguration webhookAuthenticationConfiguration,
    IRequestUtilities requestUtilities,
    ILogger<WebhookRequestAuthenticator> logger
) : IWebhookRequestAuthenticator
{
    private string _routeParameter;

    private readonly WebhookSignatureInputBuilder _webhookSignatureInputBuilder = new();

    public WebhookSignatureInputs BuildWebhookSignatureValidationInputs()
    {
        return _webhookSignatureInputBuilder.Build();
    }

    public bool TryPrepareWebhookSignatureValidation(AuthorizationFilterContext context)
    {
        var headers = context.HttpContext.Request.Headers;

        _routeParameter = requestUtilities.GetRouteParameterValue(
            context,
            context.Result,
            "bankCode"
        );

        bool checkSignatureHeader = TryReadWebhookSignatureHeader(context, headers);

        if (!checkSignatureHeader)
            return false;

        bool validateTimestampHeader = TryValidateWebhookTimestamp(context, headers);

        if (!validateTimestampHeader)
            return false;

        if (!TryGetWebhookSecretKeyBytes(context))
            return false;

        return true;
    }

    private bool TryReadWebhookSignatureHeader(
        AuthorizationFilterContext context,
        IHeaderDictionary headers
    )
    {
        var signatureHeader = webhookAuthenticationConfiguration.GetSignatureHeaderName(_routeParameter);

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
                StatusCode = (int)HttpStatusCode.BadRequest,
            };
            return false;
        }

        _webhookSignatureInputBuilder.SetSignatureBytes(receivedSignatureBytes);
        return true;
    }

    private bool TryValidateWebhookTimestamp(
        AuthorizationFilterContext context,
        IHeaderDictionary headers
    )
    {
        try
        {
            var timestampKey = webhookAuthenticationConfiguration.GetTimestampHeaderName((string)_routeParameter);

            if (
                !headers.TryGetValue(timestampKey, out var timestampHeaderValue)
                || !long.TryParse(
                    timestampHeaderValue,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var unixTimestampSeconds
                )
            )
            {
                context.Result = new BadRequestObjectResult(
                    "Missing or invalid timestamp header."
                );
                return false;
            }


            DateTimeOffset requestTimestamp;

            try
            {
                requestTimestamp =
                    DateTimeOffset.FromUnixTimeSeconds(unixTimestampSeconds);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                logger.LogWarning(
                    ex,
                    "Webhook timestamp is outside the supported Unix timestamp range.");

                context.Result = new BadRequestObjectResult(
                    "Invalid timestamp header.");

                return false;
            }


            var nowTicks = DateTime.UtcNow.Ticks;

            var minimumAllowedTimestampTicks =
                nowTicks - TimeSpan.TicksPerMinute * 3;

            var maximumAllowedTimestampTicks =
                nowTicks + TimeSpan.TicksPerMinute * 5;

            var requestTimestampTicks = DateTimeOffset
                .FromUnixTimeSeconds(unixTimestampSeconds)
                .UtcTicks;

            if (
                requestTimestampTicks < minimumAllowedTimestampTicks
                || requestTimestampTicks > maximumAllowedTimestampTicks
            )
            {
                context.Result = new BadRequestObjectResult(
                    "Timestamp is outside the allowed time window.");

                return false;
            }


            _webhookSignatureInputBuilder.SetTimestampBytes(
                webhookAuthenticationConfiguration.EncodeUtf8(timestampHeaderValue!));

            return true;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex.Message);
            throw;
        }
    }

    private bool TryGetWebhookSecretKeyBytes(AuthorizationFilterContext context)
    {
        try
        {
            var secretKey = webhookAuthenticationConfiguration.GetWebhookSecretKey((string)_routeParameter);
            var secretKeyBytes = webhookAuthenticationConfiguration.EncodeUtf8(secretKey);

            _webhookSignatureInputBuilder.SetSecretKeyBytes(secretKeyBytes);

            return true;
        }
        catch (InternalConfigurationException ex)
        {
            throw;
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