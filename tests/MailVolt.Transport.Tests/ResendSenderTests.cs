using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Transport.Resend;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MailVolt.Transport.Tests;

public sealed class ResendSenderTests
{
    private static readonly Uri ResendBaseAddress = new("https://api.resend.com");

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
    public async Task SendAsync_sends_to_emails_endpoint()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = ResendBaseAddress };

        var sender = new ResendSender(httpClient, CreateLogger());
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        capturedRequest.RequestUri!.AbsolutePath.Should().Be("/emails");
        capturedRequest.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task SendAsync_uses_snake_case_json_properties()
    {
        var capturedRequest = new HttpRequestMessage();
        var body = "";
        var handler = Helpers.HttpMessageHandlerStub.Capture((req, content) => { capturedRequest = req; body = content; });
        var httpClient = new HttpClient(handler) { BaseAddress = ResendBaseAddress };

        var sender = new ResendSender(httpClient, CreateLogger());
        var email = Helpers.CreateTestEmail() with
        {
            ReplyTo = new EmailAddress("reply@example.com")
        };

        await sender.SendAsync(email);

        using var json = JsonDocument.Parse(body);

        json.RootElement.TryGetProperty("from", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("to", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("subject", out _).Should().BeTrue();

        // Verify snake_case (not camelCase) naming
        json.RootElement.TryGetProperty("reply_to", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("replyTo", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_maps_fields_correctly()
    {
        var capturedRequest = new HttpRequestMessage();
        var body = "";
        var handler = Helpers.HttpMessageHandlerStub.Capture((req, content) => { capturedRequest = req; body = content; });
        var httpClient = new HttpClient(handler) { BaseAddress = ResendBaseAddress };

        var sender = new ResendSender(httpClient, CreateLogger());
        var email = Helpers.CreateTestEmail() with
        {
            ReplyTo = new EmailAddress("reply@example.com")
        };

        await sender.SendAsync(email);

        using var json = JsonDocument.Parse(body);

        json.RootElement.GetProperty("from").GetString().Should().Be("Sender <sender@example.com>");
        json.RootElement.GetProperty("to")[0].GetString().Should().Be("Recipient <recipient@example.com>");
        json.RootElement.GetProperty("subject").GetString().Should().Be("Test Subject");
        json.RootElement.GetProperty("html").GetString().Should().Be("<p>Hello HTML</p>");
        json.RootElement.GetProperty("text").GetString().Should().Be("Hello plain text");
        json.RootElement.GetProperty("reply_to")[0].GetString().Should().Be("reply@example.com");
    }

    [Fact]
    public async Task SendAsync_includes_attachment_as_base64_content()
    {
        var capturedRequest = new HttpRequestMessage();
        var body = "";
        var handler = Helpers.HttpMessageHandlerStub.Capture((req, content) => { capturedRequest = req; body = content; });
        var httpClient = new HttpClient(handler) { BaseAddress = ResendBaseAddress };

        var sender = new ResendSender(httpClient, CreateLogger());
        var email = Helpers.CreateTestEmailWithAttachment();

        await sender.SendAsync(email);

        using var json = JsonDocument.Parse(body);

        var attachments = json.RootElement.GetProperty("attachments").EnumerateArray().ToList();
        attachments.Should().HaveCount(1);
        attachments[0].GetProperty("filename").GetString().Should().Be("report.pdf");
        attachments[0].GetProperty("content_type").GetString().Should().Be("application/pdf");

        var base64Content = attachments[0].GetProperty("content").GetString()!;
        base64Content.Should().NotBeNullOrEmpty();

        // Verify it's valid base64
        Convert.FromBase64String(base64Content).Should().NotBeEmpty();
    }

    [Fact]
    public async Task SendAsync_includes_tags()
    {
        var capturedRequest = new HttpRequestMessage();
        var body = "";
        var handler = Helpers.HttpMessageHandlerStub.Capture((req, content) => { capturedRequest = req; body = content; });
        var httpClient = new HttpClient(handler) { BaseAddress = ResendBaseAddress };

        var sender = new ResendSender(httpClient, CreateLogger());
        var email = Helpers.CreateTestEmail() with { Tags = ["campaign-alpha", "segment-beta"] };

        await sender.SendAsync(email);

        using var json = JsonDocument.Parse(body);

        var tags = json.RootElement.GetProperty("tags").EnumerateArray().ToList();
        tags.Should().HaveCount(2);
        tags[0].GetProperty("name").GetString().Should().Be("campaign-alpha");
        tags[0].GetProperty("value").GetString().Should().Be("campaign-alpha");
        tags[1].GetProperty("name").GetString().Should().Be("segment-beta");
        tags[1].GetProperty("value").GetString().Should().Be("segment-beta");
    }

    [Fact]
    public async Task SendAsync_returns_success_with_messageId()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\": \"resend-msg-456\"}")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler) { BaseAddress = ResendBaseAddress };

        var sender = new ResendSender(httpClient, CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("resend-msg-456");
    }

    [Fact]
    public async Task SendAsync_returns_failure_on_api_error()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"statusCode\": 400, \"message\": \"Missing from\"}")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler) { BaseAddress = ResendBaseAddress };

        var sender = new ResendSender(httpClient, CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("400");
    }

    [Fact]
    public async Task SendAsync_returns_success_when_messageId_missing()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler) { BaseAddress = ResendBaseAddress };

        var sender = new ResendSender(httpClient, CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_throws_on_cancellation()
    {
        var handler = new Helpers.HttpMessageHandlerStub(_ =>
            throw new OperationCanceledException());
        var httpClient = new HttpClient(handler) { BaseAddress = ResendBaseAddress };

        var sender = new ResendSender(httpClient, CreateLogger());
        var email = Helpers.CreateTestEmail();

        Func<Task> act = () => sender.SendAsync(email);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SendAsync_includes_headers()
    {
        var capturedRequest = new HttpRequestMessage();
        var body = "";
        var handler = Helpers.HttpMessageHandlerStub.Capture((req, content) => { capturedRequest = req; body = content; });
        var httpClient = new HttpClient(handler) { BaseAddress = ResendBaseAddress };

        var sender = new ResendSender(httpClient, CreateLogger());
        var email = Helpers.CreateTestEmail() with
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Custom"] = "value-1"
            }
        };

        await sender.SendAsync(email);

        using var json = JsonDocument.Parse(body);

        json.RootElement.GetProperty("headers").GetProperty("X-Custom").GetString().Should().Be("value-1");
    }
}
