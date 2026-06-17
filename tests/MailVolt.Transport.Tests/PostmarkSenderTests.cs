using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Transport.Postmark;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PostmarkDotNet;
using Task = System.Threading.Tasks.Task;

namespace MailVolt.Transport.Tests;

public sealed class PostmarkSenderTests
{
    private static readonly PostmarkSenderOptions Options = new()
    {
        ApiKey = "my-postmark-token",
        BaseUrl = "https://api.postmarkapp.com",
        MessageStream = "outbound"
    };

    private static ILogger<PostmarkSender> CreateLogger()
        => NullLogger<PostmarkSender>.Instance;

    [Fact]
    public void PostmarkSender_implements_IPostmarkSender()
    {
        typeof(PostmarkSender).Should().Implement<IPostmarkSender>();
    }

    [Fact]
    public void IPostmarkSender_extends_ISender()
    {
        typeof(IPostmarkSender).Should().Implement<ISender>();
    }

    [Fact]
    public void Constructor_throws_on_null_client()
    {
        var act = () => new PostmarkSender(null!, Helpers.OptionsOf(Options), CreateLogger());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_throws_on_null_options()
    {
        var act = () => new PostmarkSender(Substitute.For<IPostmarkClient>(), null!, CreateLogger());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_throws_on_null_logger()
    {
        var act = () => new PostmarkSender(Substitute.For<IPostmarkClient>(), Helpers.OptionsOf(Options), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddPostmarkSender_with_delegate_registers_ISender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = new MailVoltBuilder(services);

        builder.AddPostmarkSender(opts =>
        {
            opts.ApiKey = "pm-key";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<PostmarkSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<PostmarkSenderOptions>>().Value;
        resolvedOptions.ApiKey.Should().Be("pm-key");
    }

    [Fact]
    public void AddPostmarkSender_with_configuration_registers_ISender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = new MailVoltBuilder(services);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:Postmark:ApiKey"] = "pm-key",
                ["MailVolt:Postmark:MessageStream"] = "transactional"
            })
            .Build();

        builder.AddPostmarkSender(config);

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<PostmarkSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<PostmarkSenderOptions>>().Value;
        resolvedOptions.ApiKey.Should().Be("pm-key");
        resolvedOptions.MessageStream.Should().Be("transactional");
    }

    [Fact]
    public void MapToPostmarkMessage_maps_from_to_subject_and_content()
    {
        var email = Helpers.CreateTestEmail();

        var message = PostmarkSender.MapToPostmarkMessage(email, Options);

        message.From.Should().Be("Sender <sender@example.com>");
        message.To.Should().Be("Recipient <recipient@example.com>");
        message.Subject.Should().Be("Test Subject");
        message.HtmlBody.Should().Be("<p>Hello HTML</p>");
        message.TextBody.Should().Be("Hello plain text");
    }

    [Fact]
    public void MapToPostmarkMessage_maps_reply_to()
    {
        var email = Helpers.CreateTestEmail() with
        {
            ReplyTo = new EmailAddress("reply@example.com")
        };

        var message = PostmarkSender.MapToPostmarkMessage(email, Options);

        message.ReplyTo.Should().Be("reply@example.com");
    }

    [Fact]
    public void MapToPostmarkMessage_maps_cc_and_bcc()
    {
        var email = Helpers.CreateTestEmail() with
        {
            Cc = [new EmailAddress("cc@example.com", "CC Person")],
            Bcc = [new EmailAddress("bcc@example.com")]
        };

        var message = PostmarkSender.MapToPostmarkMessage(email, Options);

        message.Cc.Should().Be("CC Person <cc@example.com>");
        message.Bcc.Should().Be("bcc@example.com");
    }

    [Fact]
    public void MapToPostmarkMessage_maps_message_stream_from_options()
    {
        var customOptions = new PostmarkSenderOptions
        {
            ApiKey = "my-postmark-token",
            BaseUrl = "https://api.postmarkapp.com",
            MessageStream = "transactional"
        };

        var message = PostmarkSender.MapToPostmarkMessage(Helpers.CreateTestEmail(), customOptions);

        message.MessageStream.Should().Be("transactional");
    }

    [Fact]
    public void MapToPostmarkMessage_maps_headers()
    {
        var email = Helpers.CreateTestEmail() with
        {
            Headers = new Dictionary<string, string> { ["X-Custom"] = "custom-value" }
        };

        var message = PostmarkSender.MapToPostmarkMessage(email, Options);

        message.Headers.Should().ContainSingle(h => h.Name == "X-Custom" && h.Value == "custom-value");
    }

    [Fact]
    public void MapToPostmarkMessage_maps_tag_from_first_tag()
    {
        var email = Helpers.CreateTestEmail() with { Tags = ["welcome-email", "onboarding"] };

        var message = PostmarkSender.MapToPostmarkMessage(email, Options);

        message.Tag.Should().Be("welcome-email");
    }

    [Fact]
    public void MapToPostmarkMessage_maps_attachment()
    {
        var email = Helpers.CreateTestEmailWithAttachment();

        var message = PostmarkSender.MapToPostmarkMessage(email, Options);

        message.Attachments.Should().HaveCount(1);
        var attachment = message.Attachments.Single();
        attachment.Name.Should().Be("report.pdf");
        attachment.ContentType.Should().Be("application/pdf");
        attachment.Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MapToPostmarkMessage_throws_when_From_is_missing()
    {
        var email = Helpers.CreateTestEmail() with { From = null };

        var act = () => PostmarkSender.MapToPostmarkMessage(email, Options);

        act.Should().Throw<InvalidOperationException>().WithMessage("*From*");
    }

    [Fact]
    public async Task SendAsync_returns_success_with_messageId()
    {
        var client = Substitute.For<IPostmarkClient>();
        client.SendMessageAsync(Arg.Any<PostmarkMessage>())
            .Returns(new PostmarkResponse
            {
                Status = PostmarkStatus.Success,
                MessageID = Guid.Parse("12345678-1234-1234-1234-123456789012")
            });

        var sender = new PostmarkSender(client, Helpers.OptionsOf(Options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("12345678-1234-1234-1234-123456789012");
        await client.Received(1).SendMessageAsync(Arg.Is<PostmarkMessage>(m => m.Subject == "Test Subject"));
    }

    [Fact]
    public async Task SendAsync_returns_failure_on_api_error()
    {
        var client = Substitute.For<IPostmarkClient>();
        client.SendMessageAsync(Arg.Any<PostmarkMessage>())
            .Returns(new PostmarkResponse
            {
                Status = PostmarkStatus.UserError,
                ErrorCode = 300,
                Message = "Missing from"
            });

        var sender = new PostmarkSender(client, Helpers.OptionsOf(Options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Missing from");
    }

    [Fact]
    public async Task SendAsync_returns_failure_when_cancelled()
    {
        var client = Substitute.For<IPostmarkClient>();
        var sender = new PostmarkSender(client, Helpers.OptionsOf(Options), CreateLogger());
        var email = Helpers.CreateTestEmail();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await sender.SendAsync(email, cts.Token);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cancelled");
        await client.DidNotReceive().SendMessageAsync(Arg.Any<PostmarkMessage>());
    }
}
