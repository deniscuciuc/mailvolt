using HandlebarsDotNet;
using AwesomeAssertions;
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Templates.Handlebars;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MailVolt.Templates.Tests;

/// <summary>
/// docs/templates/handlebars.md documented helper and partial registration that did not
/// exist — <c>UseHandlebarsTemplates</c> had no options overload at all. These cover the
/// implementation that makes those docs true.
/// </summary>
public sealed class HandlebarsCustomizationTests
{
    [Fact]
    public async Task A_registered_helper_is_callable_from_a_template()
    {
        var renderer = Renderer(options => options.RegisterHelper("uppercase",
            (writer, _, parameters) =>
                writer.WriteSafeString(parameters[0].ToString()!.ToUpperInvariant())));

        var result = await renderer.RenderAsync("{{uppercase title}}", new { title = "welcome" });

        result.Should().Be("WELCOME");
    }

    [Fact]
    public async Task A_helper_can_format_a_value()
    {
        var renderer = Renderer(options => options.RegisterHelper("formatDate",
            (writer, _, parameters) =>
                writer.WriteSafeString(
                    DateTime.Parse(parameters[0].ToString()!, System.Globalization.CultureInfo.InvariantCulture)
                        .ToString("MMMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture))));

        var result = await renderer.RenderAsync(
            "Sent on {{formatDate sentDate}}",
            new { sentDate = "2026-09-10" });

        result.Should().Be("Sent on September 10, 2026");
    }

    [Fact]
    public async Task A_registered_partial_is_usable_from_a_template()
    {
        var renderer = Renderer(options =>
            options.RegisterPartial("footer", "<p>&copy; {{year}} Example Corp</p>"));

        var result = await renderer.RenderAsync("<div>{{> footer}}</div>", new { year = 2026 });

        result.Should().Be("<div><p>&copy; 2026 Example Corp</p></div>");
    }

    [Fact]
    public async Task Multiple_helpers_and_partials_can_be_registered()
    {
        var renderer = Renderer(options => options
            .RegisterHelper("shout", (writer, _, p) => writer.WriteSafeString(p[0] + "!"))
            .RegisterPartial("greet", "Hello {{name}}"));

        var result = await renderer.RenderAsync("{{> greet}} {{shout word}}",
            new { name = "Ada", word = "hi" });

        result.Should().Be("Hello Ada hi!");
    }

    [Fact]
    public async Task Helpers_registered_on_one_renderer_do_not_leak_to_another()
    {
        // Each renderer gets its own Handlebars environment rather than mutating the global
        // one, so a library consumer cannot be affected by an unrelated registration.
        var withHelper = Renderer(options =>
            options.RegisterHelper("custom", (writer, _, _) => writer.WriteSafeString("x")));
        var withoutHelper = Renderer(configure: null);

        (await withHelper.RenderAsync("{{custom}}", new { })).Should().Be("x");

        var act = () => withoutHelper.RenderAsync("{{custom}}", new { });
        // Handlebars renders an unknown helper as empty rather than throwing, so the
        // observable difference is the output.
        (await act()).Should().BeEmpty();
    }

    [Fact]
    public void The_renderer_works_with_no_options_registered()
    {
        var services = new ServiceCollection();
        services.AddMailVolt().UseHandlebarsTemplates();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITemplateRenderer>().Should().NotBeNull();
    }

    [Fact]
    public void The_renderer_is_a_singleton_so_the_compiled_cache_is_shared()
    {
        var services = new ServiceCollection();
        services.AddMailVolt().UseHandlebarsTemplates();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITemplateRenderer>()
            .Should().BeSameAs(provider.GetRequiredService<ITemplateRenderer>());
    }

    [Fact]
    public void RegisterHelper_rejects_a_blank_name()
    {
        var act = () => new HandlebarsTemplateOptions()
            .RegisterHelper("  ", (writer, _, _) => writer.WriteSafeString("x"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterPartial_rejects_a_blank_name()
    {
        var act = () => new HandlebarsTemplateOptions().RegisterPartial("", "body");

        act.Should().Throw<ArgumentException>();
    }

    private static ITemplateRenderer Renderer(Action<HandlebarsTemplateOptions>? configure)
    {
        var services = new ServiceCollection();
        services.AddMailVolt().UseHandlebarsTemplates(configure!);

        return services.BuildServiceProvider().GetRequiredService<ITemplateRenderer>();
    }
}
