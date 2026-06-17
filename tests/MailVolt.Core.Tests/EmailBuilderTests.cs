using FluentAssertions;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace MailVolt.Core.Tests;

public sealed class EmailBuilderTests
{
    private static Options.MailVoltOptions DefaultOptions() => new()
    {
        DefaultFromAddress = "default@example.com",
        DefaultFromDisplayName = "Default Sender",
    };

    private static IOptions<Options.MailVoltOptions> CreateOptions(Options.MailVoltOptions? options = null) =>
        Microsoft.Extensions.Options.Options.Create(options ?? DefaultOptions());

    private static EmailBuilder CreateBuilder(
        Options.MailVoltOptions? options = null,
        ITemplateRenderer? templateRenderer = null,
        ISender? sender = null)
    {
        return new EmailBuilder(CreateOptions(options), templateRenderer, sender);
    }

    // ─── Chaining ──────────────────────────────────────────────────────

    [Fact]
    public async Task All_builder_methods_chain_correctly()
    {
        var builder = CreateBuilder();

        var message = await builder
            .From("from@example.com")
            .To("to@example.com")
            .Cc("cc@example.com")
            .Bcc("bcc@example.com")
            .ReplyTo("reply@example.com")
            .Subject("Hello")
            .Body("plain body")
            .HtmlBody("<p>html</p>")
            .Priority(EmailPriority.High)
            .Tag("urgent")
            .Header("X-Custom", "value")
            .BuildAsync();

        message.From!.Address.Should().Be("from@example.com");
        message.To.Should().ContainSingle(a => a.Address == "to@example.com");
        message.Cc.Should().ContainSingle(a => a.Address == "cc@example.com");
        message.Bcc.Should().ContainSingle(a => a.Address == "bcc@example.com");
        message.ReplyTo!.Address.Should().Be("reply@example.com");
        message.Subject.Should().Be("Hello");
        message.TextBody.Should().Be("plain body");
        message.HtmlBody.Should().Be("<p>html</p>");
        message.Priority.Should().Be(EmailPriority.High);
        message.Tags.Should().Contain("urgent");
        message.Headers.Should().ContainKey("X-Custom").WhoseValue.Should().Be("value");
    }

    // ─── Validation ────────────────────────────────────────────────────

    [Fact]
    public async Task BuildAsync_throws_if_no_to_address()
    {
        var builder = CreateBuilder();
        builder.Subject("Hi");

        var act = () => builder.BuildAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*At least one recipient*");
    }

    [Fact]
    public async Task BuildAsync_throws_if_subject_empty()
    {
        var builder = CreateBuilder();
        builder.To("to@example.com");

        var act = () => builder.BuildAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Subject is required*");
    }

    [Fact]
    public async Task BuildAsync_throws_if_subject_whitespace()
    {
        var builder = CreateBuilder();
        builder.To("to@example.com").Subject("   ");

        var act = () => builder.BuildAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Subject is required*");
    }

    [Fact]
    public async Task BuildAsync_throws_if_no_from_and_no_default()
    {
        var options = new Options.MailVoltOptions(); // no DefaultFromAddress
        var builder = CreateBuilder(options);
        builder.To("to@example.com").Subject("Hi");

        var act = () => builder.BuildAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*From address must be specified*");
    }

    // ─── Default From fallback ─────────────────────────────────────────

    [Fact]
    public async Task BuildAsync_applies_default_from_address()
    {
        var builder = CreateBuilder();
        builder.To("to@example.com").Subject("Hi");

        var message = await builder.BuildAsync();

        message.From!.Address.Should().Be("default@example.com");
        message.From.DisplayName.Should().Be("Default Sender");
    }

    [Fact]
    public async Task BuildAsync_uses_explicit_from_over_default()
    {
        var builder = CreateBuilder();
        builder.From("custom@example.com")
            .To("to@example.com")
            .Subject("Hi");

        var message = await builder.BuildAsync();

        message.From!.Address.Should().Be("custom@example.com");
    }

    // ─── SendAsync with ISender ────────────────────────────────────────

    [Fact]
    public async Task SendAsync_calls_ISender_SendAsync_with_correct_message()
    {
        var sender = Substitute.For<ISender>();
        sender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(EmailResult.Success("sent-1"));

        var builder = CreateBuilder(sender: sender);
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Test");

        var result = await builder.SendAsync();

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("sent-1");

        await sender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.Subject == "Test" && m.To[0].Address == "to@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_throws_if_no_sender_registered()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Test");

        var act = () => builder.SendAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ISender must be registered*");
    }

    // ─── With InMemorySender ───────────────────────────────────────────

    [Fact]
    public async Task SendAsync_with_InMemorySender_captures_email()
    {
        var sender = new InMemorySender();
        var builder = CreateBuilder(sender: sender);

        builder.From("from@test.com")
            .To("to@test.com")
            .Subject("Hello from test");

        var result = await builder.SendAsync();

        result.IsSuccess.Should().BeTrue();
        sender.SentCount.Should().Be(1);
        sender.SentEmails[0].Email.Subject.Should().Be("Hello from test");
    }

    // ─── Multiple To/Cc/Bcc ───────────────────────────────────────────

    [Fact]
    public async Task Multiple_recipients_are_all_added()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to1@example.com")
            .To("to2@example.com")
            .Cc("cc1@example.com")
            .Cc("cc2@example.com")
            .Bcc("bcc1@example.com")
            .Bcc("bcc2@example.com")
            .Subject("Group");

        var message = await builder.BuildAsync();

        message.To.Should().HaveCount(2);
        message.Cc.Should().HaveCount(2);
        message.Bcc.Should().HaveCount(2);
    }

    // ─── Headers and Tags ──────────────────────────────────────────────

    [Fact]
    public async Task Multiple_tags_are_all_present()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Tagged")
            .Tag("alpha")
            .Tag("beta")
            .Tag("gamma");

        var message = await builder.BuildAsync();

        message.Tags.Should().BeEquivalentTo("alpha", "beta", "gamma");
    }

    [Fact]
    public async Task Multiple_headers_are_all_present()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Headers")
            .Header("X-A", "1")
            .Header("X-B", "2");

        var message = await builder.BuildAsync();

        message.Headers.Should().HaveCount(2);
        message.Headers["X-A"].Should().Be("1");
        message.Headers["X-B"].Should().Be("2");
    }

    [Fact]
    public async Task Header_overwrites_existing_key()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Overwrite")
            .Header("X-ID", "first")
            .Header("X-ID", "second");

        var message = await builder.BuildAsync();

        message.Headers["X-ID"].Should().Be("second");
        message.Headers.Should().HaveCount(1);
    }

    // ─── Priority ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(EmailPriority.Low)]
    [InlineData(EmailPriority.Normal)]
    [InlineData(EmailPriority.High)]
    public async Task Priority_is_respected(EmailPriority priority)
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Prio")
            .Priority(priority);

        var message = await builder.BuildAsync();

        message.Priority.Should().Be(priority);
    }

    // ─── Body methods ──────────────────────────────────────────────────

    [Fact]
    public async Task Body_sets_text_body()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Body")
            .Body("hello world");

        var message = await builder.BuildAsync();

        message.TextBody.Should().Be("hello world");
        message.HtmlBody.Should().BeNull();
    }

    [Fact]
    public async Task TextBody_sets_text_body()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Text")
            .TextBody("plain text");

        var message = await builder.BuildAsync();

        message.TextBody.Should().Be("plain text");
        message.HtmlBody.Should().BeNull();
    }

    [Fact]
    public async Task HtmlBody_sets_html_body()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("HTML")
            .HtmlBody("<h1>Title</h1>");

        var message = await builder.BuildAsync();

        message.HtmlBody.Should().Be("<h1>Title</h1>");
        message.TextBody.Should().BeNull();
    }

    [Fact]
    public async Task Body_can_be_overwritten()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Override")
            .Body("first")
            .Body("second");

        var message = await builder.BuildAsync();

        message.TextBody.Should().Be("second");
    }

    // ─── UsingTemplate ─────────────────────────────────────────────────

    [Fact]
    public async Task UsingTemplate_renders_and_sets_html_body()
    {
        var renderer = Substitute.For<ITemplateRenderer>();
        renderer.RenderAsync("Hello {{Name}}", Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns("<p>Hello World</p>");

        var builder = CreateBuilder(templateRenderer: renderer);
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Template")
            .UsingTemplate("Hello {{Name}}", new { Name = "World" });

        var message = await builder.BuildAsync();

        message.HtmlBody.Should().Be("<p>Hello World</p>");
        await renderer.Received(1).RenderAsync(
            "Hello {{Name}}", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UsingTemplate_does_not_override_explicit_body()
    {
        var renderer = Substitute.For<ITemplateRenderer>();
        renderer.RenderAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns("<p>Rendered</p>");

        var builder = CreateBuilder(templateRenderer: renderer);
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Template")
            .HtmlBody("<p>Explicit</p>")
            .UsingTemplate("template", new { });

        var message = await builder.BuildAsync();

        // Should keep explicit body, not the rendered output
        message.HtmlBody.Should().Be("<p>Explicit</p>");
    }

    [Fact]
    public async Task UsingTemplate_with_no_renderer_does_not_throw()
    {
        var builder = CreateBuilder(templateRenderer: null);
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("NoRenderer")
            .UsingTemplate("Hello {{Name}}", new { Name = "World" });

        var act = () => builder.BuildAsync();

        await act.Should().NotThrowAsync();
    }

    // ─── Attach ────────────────────────────────────────────────────────

    [Fact]
    public async Task Attach_adds_attachment()
    {
        var bytes = "content"u8.ToArray();
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Attached")
            .Attach(a => a.FromBytes("test.txt", bytes));

        var message = await builder.BuildAsync();

        message.Attachments.Should().ContainSingle();
        message.Attachments[0].FileName.Should().Be("test.txt");
    }

    [Fact]
    public async Task Multiple_attachments_are_all_added()
    {
        var bytes1 = "a"u8.ToArray();
        var bytes2 = "b"u8.ToArray();
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("MultiAttach")
            .Attach(a => a.FromBytes("a.txt", bytes1))
            .Attach(a => a.FromBytes("b.txt", bytes2));

        var message = await builder.BuildAsync();

        message.Attachments.Should().HaveCount(2);
    }

    // ─── Cancellation ──────────────────────────────────────────────────

    [Fact]
    public async Task BuildAsync_respects_cancellation_token()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("Cancel");

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => builder.BuildAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
