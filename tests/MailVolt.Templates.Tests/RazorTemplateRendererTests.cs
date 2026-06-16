using FluentAssertions;
using MailVolt.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace MailVolt.Templates.Razor.Tests;

public sealed class RazorTemplateRendererTests
{
    private readonly IRazorViewEngine _viewEngine = Substitute.For<IRazorViewEngine>();
    private readonly ITempDataProvider _tempDataProvider = Substitute.For<ITempDataProvider>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IOptions<RazorTemplateOptions> _options;

    public RazorTemplateRendererTests()
    {
        _options = Options.Create(new RazorTemplateOptions());
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Act
        var act = () => new RazorTemplateRenderer(
            _viewEngine,
            _tempDataProvider,
            _serviceProvider,
            _options);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Renderer_ImplementsITemplateRenderer()
    {
        // Arrange
        var renderer = new RazorTemplateRenderer(
            _viewEngine,
            _tempDataProvider,
            _serviceProvider,
            _options);

        // Assert
        renderer.Should().BeAssignableTo<ITemplateRenderer>();
    }

    [Fact]
    public async Task RenderAsync_WithViewFound_RendersCorrectly()
    {
        // Arrange
        const string templateName = "WelcomeEmail";
        var model = new { UserName = "Alice" };
        const string expectedOutput = "<h1>Hello Alice</h1>";

        var view = Substitute.For<IView>();
        view.RenderAsync(Arg.Any<ViewContext>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var viewContext = callInfo.Arg<ViewContext>();
                viewContext.Writer.Write(expectedOutput);
            });

        var viewResult = ViewEngineResult.Found(templateName, view);
        _viewEngine.FindView(
                Arg.Any<ActionContext>(),
                templateName,
                false)
            .Returns(viewResult);

        var renderer = new RazorTemplateRenderer(
            _viewEngine,
            _tempDataProvider,
            _serviceProvider,
            _options);

        // Act
        var result = await renderer.RenderAsync(templateName, model);

        // Assert
        result.Should().Be(expectedOutput);

        await view.Received(1).RenderAsync(Arg.Any<ViewContext>());
    }

    [Fact]
    public async Task RenderAsync_WithViewNotFoundInFindView_FallsBackToGetView()
    {
        // Arrange
        const string templateName = "Shared/_Header";
        var model = new { Title = "Test" };
        const string expectedOutput = "<header>Test</header>";

        // First — FindView fails
        _viewEngine.FindView(
                Arg.Any<ActionContext>(),
                templateName,
                false)
            .Returns(ViewEngineResult.NotFound(templateName, [templateName]));

        // Then — GetView succeeds
        var view = Substitute.For<IView>();
        view.RenderAsync(Arg.Any<ViewContext>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var viewContext = callInfo.Arg<ViewContext>();
                viewContext.Writer.Write(expectedOutput);
            });

        var viewResult = ViewEngineResult.Found(templateName, view);
        _viewEngine.GetView(
                Arg.Any<string>(),
                templateName,
                false)
            .Returns(viewResult);

        var renderer = new RazorTemplateRenderer(
            _viewEngine,
            _tempDataProvider,
            _serviceProvider,
            _options);

        // Act
        var result = await renderer.RenderAsync(templateName, model);

        // Assert
        result.Should().Be(expectedOutput);

        _viewEngine.Received(1).FindView(
            Arg.Any<ActionContext>(),
            templateName,
            false);

        _viewEngine.Received(1).GetView(
            Arg.Any<string>(),
            templateName,
            false);
    }

    [Fact]
    public async Task RenderAsync_WithViewNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        const string templateName = "MissingView";
        var model = new { };

        _viewEngine.FindView(
                Arg.Any<ActionContext>(),
                templateName,
                false)
            .Returns(ViewEngineResult.NotFound(templateName, [templateName]));

        _viewEngine.GetView(
                Arg.Any<string>(),
                templateName,
                false)
            .Returns(ViewEngineResult.NotFound(templateName, [templateName]));

        var renderer = new RazorTemplateRenderer(
            _viewEngine,
            _tempDataProvider,
            _serviceProvider,
            _options);

        // Act
        var act = () => renderer.RenderAsync(templateName, model);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*could not be found*");
    }

    [Fact]
    public async Task RenderAsync_CancellationToken_Respected()
    {
        // Arrange
        const string templateName = "CancelView";
        var model = new { };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var view = Substitute.For<IView>();
        view.RenderAsync(Arg.Any<ViewContext>())
            .Returns(Task.FromCanceled(cts.Token));

        var viewResult = ViewEngineResult.Found(templateName, view);
        _viewEngine.FindView(
                Arg.Any<ActionContext>(),
                templateName,
                false)
            .Returns(viewResult);

        var renderer = new RazorTemplateRenderer(
            _viewEngine,
            _tempDataProvider,
            _serviceProvider,
            _options);

        // Act
        var act = () => renderer.RenderAsync(templateName, model, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenViewEngineIsNull()
    {
        // Act
        var act = () => new RazorTemplateRenderer(
            null!,
            _tempDataProvider,
            _serviceProvider,
            _options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenTempDataProviderIsNull()
    {
        // Act
        var act = () => new RazorTemplateRenderer(
            _viewEngine,
            null!,
            _serviceProvider,
            _options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenServiceProviderIsNull()
    {
        // Act
        var act = () => new RazorTemplateRenderer(
            _viewEngine,
            _tempDataProvider,
            null!,
            _options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        // Act
        var act = () => new RazorTemplateRenderer(
            _viewEngine,
            _tempDataProvider,
            _serviceProvider,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
