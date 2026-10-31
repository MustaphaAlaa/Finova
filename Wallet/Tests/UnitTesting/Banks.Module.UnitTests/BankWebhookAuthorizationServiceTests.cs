using System.Security.Cryptography;
using System.Text;
using AutoFixture;
using AutoFixture.AutoMoq;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IServices;
using Banks.Application.Services.WebhookFilter;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Exceptions;


namespace Banks.Module.UnitTests;

public class BankWebhookAuthorizationServiceTests
{
    private const string SecretKey = "test-webhook-secret";
    private const string Timestamp = "1793300000";

    private readonly IFixture _fixture =
        new Fixture().Customize(new AutoMoqCustomization());

    private TestHarness CreateHarness(
        byte[]? signatureBytes = null,
        byte[]? timestampBytes = null,
        byte[]? secretKeyBytes = null)
    {
        var authenticator =
            _fixture.Freeze<Mock<IWebhookRequestAuthenticator>>();

        var logger =
            _fixture.Freeze<Mock<ILogger<BankWebhookAuthorizationService>>>();

        var settings = new WebhookAuthenticationConfiguration(
            new ConfigurationBuilder().Build());

        var inputs = new WebhookSignatureInputs(
            signatureBytes ?? Array.Empty<byte>(),
            timestampBytes ?? Encoding.UTF8.GetBytes(Timestamp),
            secretKeyBytes ?? Encoding.UTF8.GetBytes(SecretKey));

        authenticator
            .Setup(x => x.TryPrepareWebhookSignatureValidation(
                It.IsAny<AuthorizationFilterContext>()))
            .Returns(true);

        authenticator
            .Setup(x => x.BuildWebhookSignatureValidationInputs())
            .Returns(inputs);

        var httpContext = new DefaultHttpContext();

        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        var filterContext = new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());

        var sut = new BankWebhookAuthorizationService(
            settings,
            authenticator.Object,
            logger.Object);

        return new TestHarness(
            sut,
            filterContext,
            authenticator,
            inputs);
    }

    private static byte[] CreateSignature(
        byte[] secretKey,
        byte[] timestamp,
        byte[] body)
    {
        var message = timestamp
            .Concat(Encoding.UTF8.GetBytes("."))
            .Concat(body)
            .ToArray();

        return HMACSHA256.HashData(secretKey, message);
    }

    private static void SetBody(
        AuthorizationFilterContext context,
        byte[] body)
    {
        context.HttpContext.Request.Body = new MemoryStream(body);
    }

    private sealed record TestHarness(
        BankWebhookAuthorizationService Sut,
        AuthorizationFilterContext Context,
        Mock<IWebhookRequestAuthenticator> Authenticator,
        WebhookSignatureInputs Inputs);

    // Successful authorization

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WithValidSignature_DoesNotSetAuthorizationError()
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var timestamp = Encoding.UTF8.GetBytes(Timestamp);
        var body = Encoding.UTF8.GetBytes("""{"amount":100,"reference":"TX-1"}""");

        var signature = CreateSignature(key, timestamp, body);
        var harness = CreateHarness(signature, timestamp, key);

        SetBody(harness.Context, body);

        await harness.Sut.AuthorizeWebhookRequestAsync(harness.Context);

        harness.Context.Result.Should().BeNull();
    }

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WithEmptyBodyAndValidSignature_DoesNotSetAuthorizationError()
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var timestamp = Encoding.UTF8.GetBytes(Timestamp);
        var body = Array.Empty<byte>();

        var signature = CreateSignature(key, timestamp, body);
        var harness = CreateHarness(signature, timestamp, key);

        SetBody(harness.Context, body);

        await harness.Sut.AuthorizeWebhookRequestAsync(harness.Context);

        harness.Context.Result.Should().BeNull();
    }

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WithLargeBodyAndValidSignature_DoesNotSetAuthorizationError()
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var timestamp = Encoding.UTF8.GetBytes(Timestamp);

        // Larger than the service's internal read buffer.
        var body = new byte[200_000];
        RandomNumberGenerator.Fill(body);

        var signature = CreateSignature(key, timestamp, body);
        var harness = CreateHarness(signature, timestamp, key);

        SetBody(harness.Context, body);

        await harness.Sut.AuthorizeWebhookRequestAsync(harness.Context);

        harness.Context.Result.Should().BeNull();
    }

    // Preparation failures

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WhenPreparationFails_DoesNotBuildSignatureInputs()
    {
        var harness = CreateHarness();

        harness.Authenticator
            .Setup(x => x.TryPrepareWebhookSignatureValidation(
                It.IsAny<AuthorizationFilterContext>()))
            .Returns(false);

        harness.Context.Result =
            new BadRequestObjectResult("Missing signature header.");

        await harness.Sut.AuthorizeWebhookRequestAsync(harness.Context);

        harness.Authenticator.Verify(
            x => x.BuildWebhookSignatureValidationInputs(),
            Times.Never);

        harness.Context.Result.Should()
            .BeOfType<BadRequestObjectResult>();
    }

    // Invalid signatures

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WhenSignatureDoesNotMatch_ReturnsUnauthorizedWithProblemDetails()
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var timestamp = Encoding.UTF8.GetBytes(Timestamp);
        var body = Encoding.UTF8.GetBytes("""{"amount":100}""");

        var wrongSignature = new byte[32];
        var harness = CreateHarness(wrongSignature, timestamp, key);

        SetBody(harness.Context, body);

        await harness.Sut.AuthorizeWebhookRequestAsync(harness.Context);

        var result = harness.Context.Result
            .Should()
            .BeOfType<UnauthorizedObjectResult>()
            .Which;

        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var problem = result.Value
            .Should()
            .BeOfType<ProblemDetails>()
            .Which;

        problem.Status.Should().Be(StatusCodes.Status401Unauthorized);
        problem.Title.Should().Be("Invalid Webhook Key");
        problem.Detail.Should().Be("Provide a valid webhook");
    }

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WhenSignatureLengthDiffersFromExpected_ReturnsUnauthorized()
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var timestamp = Encoding.UTF8.GetBytes(Timestamp);
        var body = Encoding.UTF8.GetBytes("payload");

        var incorrectLengthSignature = new byte[] { 1, 2, 3 };
        var harness = CreateHarness(
            incorrectLengthSignature,
            timestamp,
            key);

        SetBody(harness.Context, body);

        await harness.Sut.AuthorizeWebhookRequestAsync(harness.Context);

        harness.Context.Result
            .Should()
            .BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WhenBodyChangesAfterSignatureWasGenerated_ReturnsUnauthorized()
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var timestamp = Encoding.UTF8.GetBytes(Timestamp);

        var originalBody = Encoding.UTF8.GetBytes("original payload");
        var changedBody = Encoding.UTF8.GetBytes("modified payload");

        var signature = CreateSignature(key, timestamp, originalBody);
        var harness = CreateHarness(signature, timestamp, key);

        SetBody(harness.Context, changedBody);

        await harness.Sut.AuthorizeWebhookRequestAsync(harness.Context);

        harness.Context.Result
            .Should()
            .BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WhenTimestampChangesAfterSignatureWasGenerated_ReturnsUnauthorized()
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var originalTimestamp = Encoding.UTF8.GetBytes(Timestamp);
        var changedTimestamp = Encoding.UTF8.GetBytes("1793300001");
        var body = Encoding.UTF8.GetBytes("payload");

        var signature = CreateSignature(
            key,
            originalTimestamp,
            body);

        var harness = CreateHarness(
            signature,
            changedTimestamp,
            key);

        SetBody(harness.Context, body);

        await harness.Sut.AuthorizeWebhookRequestAsync(harness.Context);

        harness.Context.Result
            .Should()
            .BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WhenSecretKeyChangesAfterSignatureWasGenerated_ReturnsUnauthorized()
    {
        var originalKey = Encoding.UTF8.GetBytes(SecretKey);
        var changedKey = Encoding.UTF8.GetBytes("different-secret");
        var timestamp = Encoding.UTF8.GetBytes(Timestamp);
        var body = Encoding.UTF8.GetBytes("payload");

        var signature = CreateSignature(
            originalKey,
            timestamp,
            body);

        var harness = CreateHarness(
            signature,
            timestamp,
            changedKey);

        SetBody(harness.Context, body);

        await harness.Sut.AuthorizeWebhookRequestAsync(harness.Context);

        harness.Context.Result
            .Should()
            .BeOfType<UnauthorizedObjectResult>();
    }

    // Request body handling

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_AfterValidation_ResetsSeekableBodyPosition()
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var timestamp = Encoding.UTF8.GetBytes(Timestamp);
        var body = Encoding.UTF8.GetBytes("payload");

        var signature = CreateSignature(key, timestamp, body);
        var harness = CreateHarness(signature, timestamp, key);

        var stream = new MemoryStream(body);
        stream.Position = 2;
        harness.Context.HttpContext.Request.Body = stream;

        await harness.Sut.AuthorizeWebhookRequestAsync(harness.Context);

        stream.Position.Should().Be(0);
    }

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WithNonSeekableBodyAndValidSignature_DoesNotSetAuthorizationError()
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var timestamp = Encoding.UTF8.GetBytes(Timestamp);
        var body = Encoding.UTF8.GetBytes("payload");

        var signature = CreateSignature(key, timestamp, body);
        var harness = CreateHarness(signature, timestamp, key);

        harness.Context.HttpContext.Request.Body =
            new NonSeekableReadStream(body);

        await harness.Sut.AuthorizeWebhookRequestAsync(harness.Context);

        harness.Context.Result.Should().BeNull();
    }

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WhenBodyReadFails_PropagatesIOException()
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var timestamp = Encoding.UTF8.GetBytes(Timestamp);
        var harness = CreateHarness(
            new byte[32],
            timestamp,
            key);

        harness.Context.HttpContext.Request.Body =
            new ThrowingReadStream();

        var act = () => harness.Sut.AuthorizeWebhookRequestAsync(
            harness.Context);

        await act.Should().ThrowAsync<IOException>();
    }

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WhenRequestIsCancelled_PropagatesCancellation()
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var timestamp = Encoding.UTF8.GetBytes(Timestamp);
        var harness = CreateHarness(
            new byte[32],
            timestamp,
            key);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        harness.Context.HttpContext.RequestAborted = cancellation.Token;
        SetBody(harness.Context, Encoding.UTF8.GetBytes("payload"));

        var act = () => harness.Sut.AuthorizeWebhookRequestAsync(
            harness.Context);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // Configuration exceptions

    [Fact]
    public async Task AuthorizeWebhookRequestAsync_WhenPreparationThrowsInternalConfigurationException_PropagatesException()
    {
        var harness = CreateHarness();

        harness.Authenticator
            .Setup(x => x.TryPrepareWebhookSignatureValidation(
                It.IsAny<AuthorizationFilterContext>()))
            .Throws(new InternalConfigurationException("Missing configuration"));

        var act = () => harness.Sut.AuthorizeWebhookRequestAsync(
            harness.Context);

        await act.Should().ThrowAsync<InternalConfigurationException>();
    }

    private sealed class NonSeekableReadStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;

        public override long Position
        {
            get => base.Position;
            set => throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin loc)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingReadStream : MemoryStream
    {
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            throw new IOException("Simulated request body read failure.");
        }
    }
}
