using AwesomeAssertions;
using MailVolt.Core.Interfaces;
using Xunit;

namespace MailVolt.Templates.Liquid.Tests;

public sealed class LiquidTemplateRendererTests
{
    private readonly LiquidTemplateRenderer _sut = new();

    [Fact]
    public async Task RenderAsync_WithSimpleModel_RendersCorrectly()
    {
        // Arrange
        const string template = "Hello {{ name }}!";
        var model = new { name = "World" };

        // Act
        var result = await _sut.RenderAsync(template, model);

        // Assert
        result.Should().Be("Hello World!");
    }

    [Fact]
    public async Task RenderAsync_WithNullModel_RendersTemplate()
    {
        // Arrange
        const string template = "Value: {{ prop }}";

        // Act
        var result = await _sut.RenderAsync<object?>(template, null!);

        // Assert
        result.Should().Be("Value: ");
    }

    [Fact]
    public async Task RenderAsync_WithMissingVariable_RendersEmpty()
    {
        // Arrange
        const string template = "User: {{ missing }}";
        var model = new { name = "Alice" };

        // Act
        var result = await _sut.RenderAsync(template, model);

        // Assert
        result.Should().Be("User: ");
    }

    [Fact]
    public void RenderAsync_WithInvalidTemplateSyntax_ThrowsInvalidOperationException()
    {
        // Arrange
        const string template = "{% invalid %}";
        var model = new { };

        // Act
        var act = () => _sut.RenderAsync(template, model);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to parse Liquid template*");
    }

    [Fact]
    public async Task RenderAsync_WithComplexModelObject_RendersCorrectly()
    {
        // Arrange
        const string template = "{{ name }} — {{ email }}";
        var model = new { name = "Alice", email = "alice@example.com" };

        // Act
        var result = await _sut.RenderAsync(template, model);

        // Assert
        result.Should().Be("Alice — alice@example.com");
    }

    [Fact]
    public async Task RenderAsync_WithFilters_RendersCorrectly()
    {
        // Arrange
        const string template = "{{ name | upcase }}";
        var model = new { name = "hello" };

        // Act
        var result = await _sut.RenderAsync(template, model);

        // Assert
        result.Should().Be("HELLO");
    }

    [Fact]
    public async Task RenderAsync_WithLoop_RendersCorrectly()
    {
        // Arrange
        const string template = "{% for item in items %}{{ item }},{% endfor %}";
        var model = new { items = new[] { "a", "b", "c" } };

        // Act
        var result = await _sut.RenderAsync(template, model);

        // Assert
        result.Should().Be("a,b,c,");
    }

    [Fact]
    public async Task RenderAsync_ImplementsITemplateRenderer()
    {
        // Arrange
        var renderer = _sut as ITemplateRenderer;

        // Assert
        renderer.Should().NotBeNull();
    }
}
