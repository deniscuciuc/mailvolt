using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Transport.Resend;
using MailVolt.Transport.Resend.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace MailVolt.Transport.Tests;

public sealed class ResendSenderTests
{
    private static readonly ResendSenderOptions Options = new()
    {
        ApiKey = "my-resend-api-key",
        BaseUrl = "https://api.resend.com"
    };

    private static ILogger<ResendSender> CreateLogger()
        => NullLogger<ResendSender>.Instance;

    [Fact]
    public void ResendSender_implements_IResendSender()
    {
        typeof(ResendSender).Should().Implement<IResendSender>();
    }

    [Fact]
    public void IResendSender_extends_ISender()
    {
        typeof(IResendSender).Should().Implement<ISender>();
    }

    [Fact]
    public void Constructor_throws_on_null_resend()
    {
        var act = () => new ResendSender(null!, CreateLogger());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_throws_on_null_logger()
    {
        var act = () => new ResendSender(Substitute.For<global::Resend.IResend>(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseResend_with_delegate_registers_ISender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = new MailVoltBuilder(services);

        builder.UseResend(opts =>
        {
            opts.ApiKey = "resend-key";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<ResendSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<ResendSenderOptions>>().Value;
        resolvedOptions.ApiKey.Should().Be("resend-key");
    }

    [Fact]
    public void UseResend_with_configuration_registers_ISender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = new MailVoltBuilder(services);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:Resend:ApiKey"] = "resend-key"
            })
            .Build();

        builder.UseResend(config.GetSection("MailVolt:Resend"));

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<ResendSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<ResendSenderOptions>>().Value;
        resolvedOptions.ApiKey.Should().Be("resend-key");
    }

    [Fact]
    public void MapToEmailMessage_maps_from_to_subject_and_content()
    {
        var email = Helpers.CreateTestEmail();

        var message = ResendSender.MapToEmailMessage(email);

        message.From.ToString().Should().Contain("sender@example.com");
        message.To.Should().ContainSingle().Which.ToString().Should().Contain("recipient@example.com");
        message.Subject.Should().Be("Test Subject");
        message.HtmlBody.Should().Be("<p>Hello HTML</p>");
        message.TextBody.Should().Be("Hello plain text");
    }

    [Fact]
    public void MapToEmailMessage_maps_reply_to()
    {
        var email = Helpers.CreateTestEmail() with
        {
            ReplyTo = new EmailAddress("reply@example.com")
        };

        var message = ResendSender.MapToEmailMessage(email);

        message.ReplyTo.Should().ContainSingle().Which.ToString().Should().Contain("reply@example.com");
    }

    [Fact]
    public void MapToEmailMessage_maps_cc_and_bcc()
    {
        var email = Helpers.CreateTestEmail() with
        {
            Cc = [new EmailAddress("cc@example.com", "CC Person")],
            Bcc = [new EmailAddress("bcc@example.com")]
        };

        var message = ResendSender.MapToEmailMessage(email);

        message.Cc.Should().ContainSingle().Which.ToString().Should().Contain("cc@example.com");
        message.Bcc.Should().ContainSingle().Which.ToString().Should().Contain("bcc@example.com");
    }

    [Fact]
    public void MapToEmailMessage_maps_headers()
    {
        var email = Helpers.CreateTestEmail() with
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Custom"] = "custom-value"
            }
        };

        var message = ResendSender.MapToEmailMessage(email);

        message.Headers.Should().ContainKey("X-Custom").WhoseValue.Should().Be("custom-value");
    }

    [Fact]
    public void MapToEmailMessage_maps_tags()
    {
        var email = Helpers.CreateTestEmail() with { Tags = ["campaign-alpha", "segment-beta"] };

        var message = ResendSender.MapToEmailMessage(email);

        message.Tags.Should().HaveCount(2);
        message.Tags[0].Name.Should().Be("campaign-alpha");
        message.Tags[0].Value.Should().Be("campaign-alpha");
        message.Tags[1].Name.Should().Be("segment-beta");
        message.Tags[1].Value.Should().Be("segment-beta");
    }

    [Fact]
    public void MapToEmailMessage_maps_attachment_as_base64_content()
    {
        var email = Helpers.CreateTestEmailWithAttachment();

        var message = ResendSender.MapToEmailMessage(email);

        message.Attachments.Should().HaveCount(1);
        var attachment = message.Attachments.Single();
        attachment.Filename.Should().Be("report.pdf");
        attachment.ContentType.Should().Be("application/pdf");

        var content = attachment.Content!.Value.String;

        content.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(content!).Should().NotBeEmpty();
    }

    [Fact]
    public async Task SendAsync_returns_success_with_messageId()
    {
        var resend = Substitute.For<global::Resend.IResend>();
        resend.EmailSendAsync(Arg.Any<global::Resend.EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(new global::Resend.ResendResponse<Guid>(
                Guid.Parse("12345678-1234-1234-1234-123456789012"),
                new global::Resend.ResendRateLimit()));

        var sender = new ResendSender(resend, CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("12345678-1234-1234-1234-123456789012");
        await resend.Received(1).EmailSendAsync(
            Arg.Is<global::Resend.EmailMessage>(m => m.Subject == "Test Subject"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_returns_success_when_messageId_missing()
    {
        var resend = Substitute.For<global::Resend.IResend>();
        resend.EmailSendAsync(Arg.Any<global::Resend.EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(new global::Resend.ResendResponse<Guid>(
                Guid.Empty,
                new global::Resend.ResendRateLimit()));

        var sender = new ResendSender(resend, CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_returns_failure_on_api_error()
    {
        var resend = Substitute.For<global::Resend.IResend>();
        resend.EmailSendAsync(Arg.Any<global::Resend.EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(new global::Resend.ResendResponse<Guid>(
                new global::Resend.ResendException(System.Net.HttpStatusCode.BadRequest, global::Resend.ErrorType.ValidationError, "Missing from", new global::Resend.ResendRateLimit()),
                new global::Resend.ResendRateLimit()));

        var sender = new ResendSender(resend, CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Missing from");
    }

    [Fact]
    public async Task SendAsync_throws_on_cancellation()
    {
        var resend = Substitute.For<global::Resend.IResend>();
        resend.EmailSendAsync(Arg.Any<global::Resend.EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<global::Resend.ResendResponse<Guid>>(new OperationCanceledException()));

        var sender = new ResendSender(resend, CreateLogger());
        var email = Helpers.CreateTestEmail();

        Func<Task> act = () => sender.SendAsync(email);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
