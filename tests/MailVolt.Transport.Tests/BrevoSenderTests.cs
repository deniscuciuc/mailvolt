using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Transport.Brevo;
using Microsoft.Extensions.Options;
using Xunit;

namespace MailVolt.Transport.Tests;

public sealed class BrevoSenderTests
{
    private static readonly Uri BrevoBaseAddress = new("https://api.brevo.com");

    private readonly BrevoSenderOptions _options = new()
    {
        ApiKey = "my-brevo-api-key"
    };

    [Fact]
    public void BrevoSender_implements_IBrevoSender()
    {
        typeof(BrevoSender).Should().Implement<IBrevoSender>();
    }

    [Fact]
    public void IBrevoSender_extends_ISender()
    {
        typeof(IBrevoSender).Should().Implement<ISender>();
    }

    [Fact]
    public async Task SendAsync_sends_to_v3_smtp_email_endpoint()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = BrevoBaseAddress };
        var sender = new BrevoSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        capturedRequest.RequestUri!.AbsolutePath.Should().Be("/v3/smtp/email");
        capturedRequest.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task SendAsync_sets_api_key_header()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = BrevoBaseAddress };

        var sender = new BrevoSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        capturedRequest.Headers.Should().ContainKey("api-key");
        capturedRequest.Headers.GetValues("api-key").Should().Contain("my-brevo-api-key");
    }

    [Fact]
    public async Task SendAsync_maps_sender_correctly()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = BrevoBaseAddress };

        var sender = new BrevoSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        json.RootElement.GetProperty("sender").GetProperty("email").GetString().Should().Be("sender@example.com");
        json.RootElement.GetProperty("sender").GetProperty("name").GetString().Should().Be("Sender");
    }

    [Fact]
    public async Task SendAsync_maps_recipients_correctly()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = BrevoBaseAddress };

        var sender = new BrevoSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        var to = json.RootElement.GetProperty("to").EnumerateArray().ToList();
        to.Should().HaveCount(1);
        to[0].GetProperty("email").GetString().Should().Be("recipient@example.com");
        to[0].GetProperty("name").GetString().Should().Be("Recipient");
    }

    [Fact]
    public async Task SendAsync_maps_subject_and_content()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = BrevoBaseAddress };

        var sender = new BrevoSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        json.RootElement.GetProperty("subject").GetString().Should().Be("Test Subject");
        json.RootElement.GetProperty("htmlContent").GetString().Should().Be("<p>Hello HTML</p>");
        json.RootElement.GetProperty("textContent").GetString().Should().Be("Hello plain text");
    }

    [Fact]
    public async Task SendAsync_includes_attachment()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = BrevoBaseAddress };

        var sender = new BrevoSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmailWithAttachment();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        var attachments = json.RootElement.GetProperty("attachment").EnumerateArray().ToList();
        attachments.Should().HaveCount(1);
        attachments[0].GetProperty("name").GetString().Should().Be("report.pdf");
        attachments[0].GetProperty("content").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendAsync_maps_reply_to()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = BrevoBaseAddress };

        var sender = new BrevoSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail() with
        {
            ReplyTo = new EmailAddress("reply@example.com", "Reply Person")
        };

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        json.RootElement.GetProperty("replyTo").GetProperty("email").GetString().Should().Be("reply@example.com");
        json.RootElement.GetProperty("replyTo").GetProperty("name").GetString().Should().Be("Reply Person");
    }

    [Fact]
    public async Task SendAsync_maps_cc_and_bcc()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = BrevoBaseAddress };

        var sender = new BrevoSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail() with
        {
            Cc = [new EmailAddress("cc@example.com", "CC Person")],
            Bcc = [new EmailAddress("bcc@example.com")]
        };

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        json.RootElement.GetProperty("cc")[0].GetProperty("email").GetString().Should().Be("cc@example.com");
        json.RootElement.GetProperty("bcc")[0].GetProperty("email").GetString().Should().Be("bcc@example.com");
    }

    [Fact]
    public async Task SendAsync_returns_success_with_messageId()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"messageId\": \"brevo-msg-123\"}")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler) { BaseAddress = BrevoBaseAddress };

        var sender = new BrevoSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("brevo-msg-123");
    }

    [Fact]
    public async Task SendAsync_returns_failure_on_api_error()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"code\": \"unauthorized\", \"message\": \"Invalid API key\"}")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler) { BaseAddress = BrevoBaseAddress };

        var sender = new BrevoSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Constructor_throws_on_null_httpClient()
    {
        var act = () => new BrevoSender(null!, Helpers.OptionsOf(_options));
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_throws_on_null_options()
    {
        var act = () => new BrevoSender(new HttpClient(), null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
