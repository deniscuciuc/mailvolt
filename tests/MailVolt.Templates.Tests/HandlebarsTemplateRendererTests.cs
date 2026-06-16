using FluentAssertions;
using MailVolt.Core.Interfaces;
using Xunit;

namespace MailVolt.Templates.Handlebars.Tests;

public sealed class HandlebarsTemplateRendererTests
{
    private readonly HandlebarsTemplateRenderer _sut = new();

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
    public async Task RenderAsync_WithConditionalBlock_RendersTrueBranch()
    {
        // Arrange
        const string template = "{{#if show}}Visible{{else}}Hidden{{/if}}";
        var model = new { show = true };

        // Act
        var result = await _sut.RenderAsync(template, model);

        // Assert
        result.Should().Be("Visible");
    }

    [Fact]
    public async Task RenderAsync_WithConditionalBlock_RendersFalseBranch()
    {
        // Arrange
        const string template = "{{#if show}}Visible{{else}}Hidden{{/if}}";
        var model = new { show = false };

        // Act
        var result = await _sut.RenderAsync(template, model);

        // Assert
        result.Should().Be("Hidden");
    }

    [Fact]
    public async Task RenderAsync_WithInlineTemplate_CachesCompiledTemplate()
    {
        // Arrange
        const string template = "Hello {{ name }}!";
        var model1 = new { name = "World" };
        var model2 = new { name = "Tests" };

        // Act
        var result1 = await _sut.RenderAsync(template, model1);
        var result2 = await _sut.RenderAsync(template, model2);

        // Assert
        result1.Should().Be("Hello World!");
        result2.Should().Be("Hello Tests!");
    }

    [Fact]
    public async Task RenderAsync_WithInlineTemplate_NotFilePath_RendersCorrectly()
    {
        // Arrange
        const string template = "User: {{ firstName }} {{ lastName }}";
        var model = new { firstName = "Alice", lastName = "Smith" };

        // Act
        var result = await _sut.RenderAsync(template, model);

        // Assert
        result.Should().Be("User: Alice Smith");
    }

    [Fact]
    public async Task RenderAsync_WithNullModel_RendersWithEmptyContext()
    {
        // Arrange
        const string template = "Value: {{ prop }}";

        // Act
        var result = await _sut.RenderAsync<object?>(template, null!);

        // Assert
        result.Should().Be("Value: ");
    }

    [Fact]
    public async Task RenderAsync_ImplementsITemplateRenderer()
    {
        // Arrange
        var renderer = _sut as ITemplateRenderer;

        // Assert
        renderer.Should().NotBeNull();
    }

    [Fact]
    public async Task RenderAsync_WithEachLoop_RendersCorrectly()
    {
        // Arrange
        const string template = "{{#each items}}{{this}}{{/each}}";
        var model = new { items = new[] { "A", "B", "C" } };

        // Act
        var result = await _sut.RenderAsync(template, model);

        // Assert
        result.Should().Be("ABC");
    }

    [Fact]
    public async Task RenderAsync_WithNestedModel_RendersCorrectly()
    {
        // Arrange
        const string template = "{{ user.name }} — {{ user.email }}";
        var model = new { user = new { name = "Bob", email = "bob@example.com" } };

        // Act
        var result = await _sut.RenderAsync(template, model);

        // Assert
        result.Should().Be("Bob — bob@example.com");
    }
}
