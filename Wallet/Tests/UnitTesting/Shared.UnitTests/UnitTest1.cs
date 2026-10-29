using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Shared.Services;

namespace Shared.UnitTests;

using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Xunit;

public class RequestUtilitiesTests
{
    private readonly IFixture _fixture = new Fixture()
        .Customize(new AutoMoqCustomization());

    private readonly RequestUtilities _sut = new();

    [Fact]
    public void GetRouteParameterValue_WhenRouteParameterExists_ReturnsItsValue()
    {
        // Arrange
        var context = CreateAuthorizationFilterContext();
        context.RouteData.Values["bankCode"] = "Acme";

        // Act
        var result = _sut.GetRouteParameterValue(
            context,
            context.Result,
            "bankCode");

        // Assert
        result.Should().Be("Acme");
    }

    [Fact]
    public void GetRouteParameterValue_WhenRouteParameterIsMissing_ReturnsNull()
    {
        // Arrange
        var context = CreateAuthorizationFilterContext();

        // Act
        var result = _sut.GetRouteParameterValue(
            context,
            context.Result,
            "bankCode");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void GetRouteParameterValue_WhenRouteParameterIsWhitespace_ReturnsNull(
        string bankCode)
    {
        // Arrange
        var context = CreateAuthorizationFilterContext();
        context.RouteData.Values["bankCode"] = bankCode;

        // Act
        var result = _sut.GetRouteParameterValue(
            context,
            context.Result,
            "bankCode");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetRouteParameterValue_WhenRouteParameterIsNotAString_ReturnsNull()
    {
        // Arrange
        var context = CreateAuthorizationFilterContext();
        context.RouteData.Values["bankCode"] = 123;

        // Act
        var result = _sut.GetRouteParameterValue(
            context,
            context.Result,
            "bankCode");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetRouteParameterValue_WhenActionExecutingContextContainsBankCode_ReturnsItsValue()
    {
        // Arrange
        var context = CreateActionExecutingContext();
        context.RouteData.Values["bankCode"] = "Acme";

        // Act
        var result = _sut.GetRouteParameterValue(
            context,
            context.Result,
            "bankCode");

        // Assert
        result.Should().Be("Acme");
    }

    private static AuthorizationFilterContext CreateAuthorizationFilterContext()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());
    }

    private static ActionExecutingContext CreateActionExecutingContext()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }
}