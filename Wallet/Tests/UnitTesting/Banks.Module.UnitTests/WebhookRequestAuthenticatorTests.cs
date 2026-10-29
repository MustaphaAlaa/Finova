using System.Globalization;
using System.Text;
using AutoFixture;
using AutoFixture.AutoMoq;
using Banks.Application.Services.WebhookFilter;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Interfaces.IServices;

namespace Banks.Module.UnitTests;

public class WebhookRequestAuthenticatorTests
{
    private const string BankCode = "Acme";
    private const string SignatureHeader = "X-Webhook-Signature";
    private const string TimestampHeader = "X-Webhook-Timestamp";
    private const string SecretKey = "test-webhook-secret";
    private const string ValidSignature = "A1B2C3D4";

    private TestHarness CreateHarness(
        string? bankCode = BankCode,
        IDictionary<string, string?>? configurationOverrides = null)
    {
        var fixture = new Fixture()
            .Customize(new AutoMoqCustomization());

        var requestUtilities = fixture.Freeze<Mock<IRequestUtilities>>();

        var logger = NullLogger<WebhookRequestAuthenticator>.Instance;

        var configurationValues = new Dictionary<string, string?>
        {
            [$"WebhooksHeaders:{BankCode}:Signature"] = SignatureHeader,
            [$"WebhooksHeaders:{BankCode}:Timestamp"] = TimestampHeader,
            [$"WebhooksHeaders:{BankCode}:SecretKey"] = SecretKey
        };

        if (configurationOverrides is not null)
        {
            foreach (var item in configurationOverrides)
            {
                configurationValues[item.Key] = item.Value;
            }
        }

        var configuration = new WebhookAuthenticationConfiguration(
            new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build());

        var httpContext = new DefaultHttpContext();

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var authorizationContext = new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());

        requestUtilities
            .Setup(x => x.GetRouteParameterValue(
                authorizationContext,
                null,
                "bankCode"))
            .Returns(bankCode);

        var sut = new WebhookRequestAuthenticator(
            configuration,
            requestUtilities.Object,
            logger);

        return new TestHarness(
            sut,
            authorizationContext,
            requestUtilities,
            logger);
    }

    private static void AddValidHeaders(
        AuthorizationFilterContext context,
        long? timestamp = null)
    {
        context.HttpContext.Request.Headers[SignatureHeader] = ValidSignature;

        context.HttpContext.Request.Headers[TimestampHeader] =
            (timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            .ToString(CultureInfo.InvariantCulture);
    }

    private static BadRequestObjectResult GetBadRequest(
        AuthorizationFilterContext context)
    {
        return context.Result
            .Should()
            .BeOfType<BadRequestObjectResult>()
            .Which;
    }

    private sealed record TestHarness(
        WebhookRequestAuthenticator Sut,
        AuthorizationFilterContext Context,
        Mock<IRequestUtilities> RequestUtilities,
         ILogger<WebhookRequestAuthenticator> Logger);

    // Successful requests

    [Fact]
    public void TryPrepareWebhookSignatureValidation_WithValidHeaders_ReturnsTrue()
    {
        var harness = CreateHarness();
        AddValidHeaders(harness.Context);

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeTrue();
        harness.Context.Result.Should().BeNull();
    }

    [Fact]
    public void BuildWebhookSignatureValidationInputs_AfterSuccessfulPreparation_ReturnsDecodedSignatureAndUtf8Inputs()
    {
        var harness = CreateHarness();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);

        harness.Context.HttpContext.Request.Headers[SignatureHeader] =
            ValidSignature;

        harness.Context.HttpContext.Request.Headers[TimestampHeader] =
            timestamp;

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeTrue();

        var inputs = harness.Sut.BuildWebhookSignatureValidationInputs();

        inputs.SignatureBytes.Should().Equal(
            Convert.FromHexString(ValidSignature));

        inputs.TimestampBytes.Should().Equal(
            Encoding.UTF8.GetBytes(timestamp));

        inputs.SecretKeyBytes.Should().Equal(
            Encoding.UTF8.GetBytes(SecretKey));
    }

    [Fact]
    public void TryPrepareWebhookSignatureValidation_WithTimestampWithinPastWindow_ReturnsTrue()
    {
        var harness = CreateHarness();

        var timestamp = DateTimeOffset.UtcNow
            .AddMinutes(-2)
            .ToUnixTimeSeconds();

        AddValidHeaders(harness.Context, timestamp);

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeTrue();
        harness.Context.Result.Should().BeNull();
    }

    [Fact]
    public void TryPrepareWebhookSignatureValidation_WithTimestampWithinFutureWindow_ReturnsTrue()
    {
        var harness = CreateHarness();

        var timestamp = DateTimeOffset.UtcNow
            .AddMinutes(4)
            .ToUnixTimeSeconds();

        AddValidHeaders(harness.Context, timestamp);

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeTrue();
        harness.Context.Result.Should().BeNull();
    }

    // Signature header validation

    [Fact]
    public void TryPrepareWebhookSignatureValidation_WhenSignatureHeaderIsMissing_ReturnsBadRequest()
    {
        var harness = CreateHarness();

        harness.Context.HttpContext.Request.Headers[TimestampHeader] =
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                .ToString(CultureInfo.InvariantCulture);

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeFalse();

        var response = GetBadRequest(harness.Context);
        response.Value.Should().Be(
            "Missing or empty X-Webhook-Signature header.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void TryPrepareWebhookSignatureValidation_WhenSignatureHeaderIsEmptyOrWhitespace_ReturnsBadRequest(
        string signature)
    {
        var harness = CreateHarness();
        AddValidHeaders(harness.Context);

        harness.Context.HttpContext.Request.Headers[SignatureHeader] =
            signature;

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeFalse();

        GetBadRequest(harness.Context).Value.Should().Be(
            "Missing or empty X-Webhook-Signature header.");
    }

    [Theory]
    [InlineData("XYZ")]
    [InlineData("ABC")]
    [InlineData("GG")]
    public void TryPrepareWebhookSignatureValidation_WhenSignatureIsNotValidHex_ReturnsBadRequest(
        string signature)
    {
        var harness = CreateHarness();
        AddValidHeaders(harness.Context);

        harness.Context.HttpContext.Request.Headers[SignatureHeader] =
            signature;

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeFalse();

        var response = GetBadRequest(harness.Context);
        response.Value.Should().Be("Provide valid webhook signature.");
        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    // Timestamp header validation

    [Fact]
    public void TryPrepareWebhookSignatureValidation_WhenTimestampHeaderIsMissing_ReturnsBadRequest()
    {
        var harness = CreateHarness();

        harness.Context.HttpContext.Request.Headers[SignatureHeader] =
            ValidSignature;

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeFalse();

        GetBadRequest(harness.Context).Value.Should().Be(
            "Missing or invalid timestamp header.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123abc")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData(" 123")]
    [InlineData("123 ")]
    public void TryPrepareWebhookSignatureValidation_WhenTimestampIsNotAnUnsignedInteger_ReturnsBadRequest(
        string timestamp)
    {
        var harness = CreateHarness();
        AddValidHeaders(harness.Context);

        harness.Context.HttpContext.Request.Headers[TimestampHeader] =
            timestamp;

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeFalse();

        GetBadRequest(harness.Context).Value.Should().Be(
            "Missing or invalid timestamp header.");
    }

    [Theory]
    [InlineData(long.MinValue)]
    public void TryPrepareWebhookSignatureValidation_WhenTimestampExceedsUnixRange_ReturnsBadRequest(
        long timestamp)
    {
        var harness = CreateHarness();
        AddValidHeaders(harness.Context, timestamp);

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeFalse();

        GetBadRequest(harness.Context).Value.Should().Be(
            "Missing or invalid timestamp header.");
    }

    [Fact]
    public void TryPrepareWebhookSignatureValidation_WhenTimestampIsTooOld_ReturnsBadRequest()
    {
        var harness = CreateHarness();

        var timestamp = DateTimeOffset.UtcNow
            .AddMinutes(-4)
            .ToUnixTimeSeconds();

        AddValidHeaders(harness.Context, timestamp);

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeFalse();

        GetBadRequest(harness.Context).Value.Should().Be(
            "Timestamp is outside the allowed time window.");
    }

    [Fact]
    public void TryPrepareWebhookSignatureValidation_WhenTimestampIsTooFarInFuture_ReturnsBadRequest()
    {
        var harness = CreateHarness();

        var timestamp = DateTimeOffset.UtcNow
            .AddMinutes(6)
            .ToUnixTimeSeconds();

        AddValidHeaders(harness.Context, timestamp);

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeFalse();

        GetBadRequest(harness.Context).Value.Should().Be(
            "Timestamp is outside the allowed time window.");
    }

    // Validation order and early exits

    [Fact]
    public void TryPrepareWebhookSignatureValidation_WhenSignatureIsInvalid_DoesNotValidateTimestampOrLoadSecret()
    {
        var harness = CreateHarness();

        harness.Context.HttpContext.Request.Headers[SignatureHeader] =
            "NOT-HEX";

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeFalse();

        harness.RequestUtilities.Verify(
            x => x.GetRouteParameterValue(
                harness.Context,
                null,
                "bankCode"),
            Times.Once);

        GetBadRequest(harness.Context).Value.Should().Be(
            "Provide valid webhook signature.");

        // A malformed signature must stop preparation before the
        // timestamp and secret key can be used.
        harness.Context.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void TryPrepareWebhookSignatureValidation_WhenTimestampIsInvalid_DoesNotProduceSuccessfulInputs()
    {
        var harness = CreateHarness();

        harness.Context.HttpContext.Request.Headers[SignatureHeader] =
            ValidSignature;

        harness.Context.HttpContext.Request.Headers[TimestampHeader] =
            "invalid";

        var result = harness.Sut.TryPrepareWebhookSignatureValidation(
            harness.Context);

        result.Should().BeFalse();

        GetBadRequest(harness.Context).Value.Should().Be(
            "Missing or invalid timestamp header.");
    }
}
