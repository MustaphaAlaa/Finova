using System.Text;
using AutoFixture;
using AutoFixture.AutoMoq;
using Banks.Application.Services.WebhookFilter;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Shared.Exceptions;


namespace Banks.Module.UnitTests;

public class WebhookAuthenticationConfigurationTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IConfiguration> _configuration;
    private readonly WebhookAuthenticationConfiguration _sut;

    public WebhookAuthenticationConfigurationTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());

        _configuration = _fixture.Freeze<Mock<IConfiguration>>();

        _sut = _fixture.Create<WebhookAuthenticationConfiguration>();
    }

    // EncodeUtf8

    [Fact]
    public void EncodeUtf8_WhenValueContainsAsciiCharacters_ReturnsUtf8Bytes()
    {
        // Arrange
        const string value = "Bank123";

        // Act
        var result = _sut.EncodeUtf8(value);

        // Assert
        result.Should().Equal(Encoding.UTF8.GetBytes(value));
        // Catches incorrect encoding or corrupted output.
    }

    [Fact]
    public void EncodeUtf8_WhenValueContainsNonAsciiCharacters_ReturnsCorrectUtf8Bytes()
    {
        // Arrange
        const string value = "Café مرحبا €";

        // Act
        var result = _sut.EncodeUtf8(value);

        // Assert
        result.Should().Equal(Encoding.UTF8.GetBytes(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void EncodeUtf8_WhenValueIsNullOrWhitespace_ThrowsArgumentException(
        string? value)
    {
        // Act
        var act = () => _sut.EncodeUtf8(value!);

        // Assert
        var exception = act.Should()
            .Throw<ArgumentException>()
            .Which;

        exception.ParamName.Should().Be("value");
        exception.Message.Should().Contain(
            "Value cannot be null or empty or whitespace.");
    }

    // GetSignatureHeaderName

    [Fact]
    public void GetSignatureHeaderName_WhenConfigured_ReturnsConfiguredValue()
    {
        // Arrange
        var bankCode = _fixture.Create<string>();
        const string expected = "X-Signature";

        _configuration
            .Setup(c => c[$"WebhooksHeaders:{bankCode}:Signature"])
            .Returns(expected);

        // Act
        var result = _sut.GetSignatureHeaderName(bankCode);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetSignatureHeaderName_WhenConfigurationIsMissing_ThrowsInternalConfigurationException()
    {
        // Arrange
        var bankCode = _fixture.Create<string>();

        _configuration
            .Setup(c => c[$"WebhooksHeaders:{bankCode}:Signature"])
            .Returns((string?)null);

        // Act
        var act = () => _sut.GetSignatureHeaderName(bankCode);

        // Assert
        act.Should()
            .Throw<InternalConfigurationException>()
            .WithMessage(
                $"Missing signature header configuration for bank '{bankCode}'.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void GetSignatureHeaderName_WhenConfigurationIsEmptyOrWhitespace_ThrowsInternalConfigurationException(
        string value)
    {
        // Arrange
        var bankCode = _fixture.Create<string>();

        _configuration
            .Setup(c => c[$"WebhooksHeaders:{bankCode}:Signature"])
            .Returns(value);

        // Act
        var act = () => _sut.GetSignatureHeaderName(bankCode);

        // Assert
        act.Should()
            .Throw<InternalConfigurationException>()
            .WithMessage(
                $"Missing signature header configuration for bank '{bankCode}'.");
    }

    // GetTimestampHeaderName

    [Fact]
    public void GetTimestampHeaderName_WhenConfigured_ReturnsConfiguredValue()
    {
        // Arrange
        var bankCode = _fixture.Create<string>();
        const string expected = "X-Timestamp";

        _configuration
            .Setup(c => c[$"WebhooksHeaders:{bankCode}:Timestamp"])
            .Returns(expected);

        // Act
        var result = _sut.GetTimestampHeaderName(bankCode);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetTimestampHeaderName_WhenConfigurationIsMissing_ThrowsInternalConfigurationException()
    {
        // Arrange
        var bankCode = _fixture.Create<string>();

        _configuration
            .Setup(c => c[$"WebhooksHeaders:{bankCode}:Timestamp"])
            .Returns((string?)null);

        // Act
        var act = () => _sut.GetTimestampHeaderName(bankCode);

        // Assert
        act.Should()
            .Throw<InternalConfigurationException>()
            .WithMessage(
                $"Missing timestamp header configuration for bank '{bankCode}'.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void GetTimestampHeaderName_WhenConfigurationIsEmptyOrWhitespace_ThrowsInternalConfigurationException(
        string value)
    {
        // Arrange
        var bankCode = _fixture.Create<string>();

        _configuration
            .Setup(c => c[$"WebhooksHeaders:{bankCode}:Timestamp"])
            .Returns(value);

        // Act
        var act = () => _sut.GetTimestampHeaderName(bankCode);

        // Assert
        act.Should()
            .Throw<InternalConfigurationException>()
            .WithMessage(
                $"Missing timestamp header configuration for bank '{bankCode}'.");
    }

    // GetWebhookSecretKey

    [Fact]
    public void GetWebhookSecretKey_WhenConfigured_ReturnsConfiguredValue()
    {
        // Arrange
        var bankCode = _fixture.Create<string>();
        var expected = _fixture.Create<string>();

        _configuration
            .Setup(c => c[$"WebhooksHeaders:{bankCode}:SecretKey"])
            .Returns(expected);

        // Act
        var result = _sut.GetWebhookSecretKey(bankCode);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetWebhookSecretKey_WhenConfigurationIsMissing_ThrowsInternalConfigurationException()
    {
        // Arrange
        var bankCode = _fixture.Create<string>();

        _configuration
            .Setup(c => c[$"WebhooksHeaders:{bankCode}:SecretKey"])
            .Returns((string?)null);

        // Act
        var act = () => _sut.GetWebhookSecretKey(bankCode);

        // Assert
        act.Should()
            .Throw<InternalConfigurationException>()
            .WithMessage(
                $"Missing secret key configuration for bank '{bankCode}'.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void GetWebhookSecretKey_WhenConfigurationIsEmptyOrWhitespace_ThrowsInternalConfigurationException(
        string value)
    {
        // Arrange
        var bankCode = _fixture.Create<string>();

        _configuration
            .Setup(c => c[$"WebhooksHeaders:{bankCode}:SecretKey"])
            .Returns(value);

        // Act
        var act = () => _sut.GetWebhookSecretKey(bankCode);

        // Assert
        act.Should()
            .Throw<InternalConfigurationException>()
            .WithMessage(
                $"Missing secret key configuration for bank '{bankCode}'.");
    }

    // Bank-specific configuration isolation

    [Fact]
    public void GetSignatureHeaderName_WhenMultipleBanksAreConfigured_ReturnsValueForRequestedBank()
    {
        // Arrange
        const string firstBank = "Acme";
        const string secondBank = "PayTech";

        _configuration
            .Setup(c => c["WebhooksHeaders:Acme:Signature"])
            .Returns("X-Acme-Signature");

        _configuration
            .Setup(c => c["WebhooksHeaders:PayTech:Signature"])
            .Returns("X-PayTech-Signature");

        // Act
        var result = _sut.GetSignatureHeaderName(secondBank);

        // Assert
        result.Should().Be("X-PayTech-Signature");
    }
}