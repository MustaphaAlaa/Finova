using Shared.Exceptions;


using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json; 

namespace Banks.Module.UnitTests;

public class InternalConfigurationExceptionHandlerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ILogger<InternalConfigurationExceptionHandler>> _logger;
    private readonly InternalConfigurationExceptionHandler _sut;

    public InternalConfigurationExceptionHandlerTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());

        _logger = _fixture.Freeze<
            Mock<ILogger<InternalConfigurationExceptionHandler>>>();

        _sut = _fixture.Create<InternalConfigurationExceptionHandler>();
    }

    [Fact]
    public async Task TryHandleAsync_WhenExceptionIsInternalConfigurationException_ReturnsTrueAndWritesProblemDetails()
    {
        // Arrange
        var exception = new InternalConfigurationException(
            "Missing webhook secret.");

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/Acme/transaction";
        context.Response.Body = new MemoryStream();

        // Act
        var result = await _sut.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(
            StatusCodes.Status500InternalServerError);

        context.Response.ContentType.Should().StartWith(
            "application/json");

        context.Response.Body.Position = 0;

        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        problem.Should().NotBeNull();
        problem!.Title.Should().Be(
            "An unexpected error occurred, we're working to solve this porblem.");
    }

    [Fact]
    public async Task TryHandleAsync_WhenExceptionIsNotInternalConfigurationException_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new InvalidOperationException("Unexpected failure.");

        // Act
        var result = await _sut.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        context.Response.StatusCode.Should().Be(
            StatusCodes.Status200OK);

        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task TryHandleAsync_WhenInternalConfigurationExceptionOccurs_LogsTheException()
    {
        // Arrange
        var exception = new InternalConfigurationException(
            "Missing webhook secret.");

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/Acme/transaction";

        // Act
        await _sut.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        // Assert
        _logger.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains(
                        "An unexpected error occurred while processing POST /Acme/transaction.")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
