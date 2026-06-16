using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.SendGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace MailVolt.Transport.Tests;

public sealed class SendGridSenderTests
{
    private readonly SendGridSenderOptions _options = new()
    {
        ApiKey = "my-sendgrid-api-key",
        BaseUrl = "https://api.sendgrid.com"
    };

    private static readonly Uri SendGridBaseAddress = new("https://api.sendgrid.com");

    private static ILogger<SendGridSender> CreateLogger()
        => NullLogger<SendGridSender>.Instance;

    [Fact]
    public void SendGridSender_implements_ISendGridSender()
    {
        typeof(SendGridSender).Should().Implement<ISendGridSender>();
    }

    [Fact]
    public void ISendGridSender_extends_ISender()
    {
        typeof(ISendGridSender).Should().Implement<ISender>();
    }

    [Fact]
    public async Task SendAsync_sends_to_v3_mail_send_endpoint()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = SendGridBaseAddress };

        var sender = new SendGridSender(httpClient, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        capturedRequest.RequestUri!.AbsolutePath.Should().Be("/v3/mail/send");
        capturedRequest.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task Bearer_auth_header_is_sent_with_request()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = SendGridBaseAddress };
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var sender = new SendGridSender(httpClient, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        capturedRequest.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization.Parameter.Should().Be("my-sendgrid-api-key");
    }

    [Fact]
    public async Task SendAsync_includes_from_and_subject_in_payload()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = SendGridBaseAddress };

        var sender = new SendGridSender(httpClient, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("from").GetProperty("email").GetString().Should().Be("sender@example.com");
        json.RootElement.GetProperty("from").GetProperty("name").GetString().Should().Be("Sender");
        json.RootElement.GetProperty("subject").GetString().Should().Be("Test Subject");
    }

    [Fact]
    public async Task SendAsync_includes_personalizations()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = SendGridBaseAddress };

        var sender = new SendGridSender(httpClient, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var personalizations = json.RootElement.GetProperty("personalizations").EnumerateArray().ToList();

        personalizations.Should().HaveCount(1);
        var to = personalizations[0].GetProperty("to").EnumerateArray().ToList();
        to.Should().HaveCount(1);
        to[0].GetProperty("email").GetString().Should().Be("recipient@example.com");
        to[0].GetProperty("name").GetString().Should().Be("Recipient");
    }

    [Fact]
    public async Task SendAsync_includes_content_for_text_and_html()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = SendGridBaseAddress };

        var sender = new SendGridSender(httpClient, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var content = json.RootElement.GetProperty("content").EnumerateArray().ToList();

        content.Should().HaveCount(2);
        content.Should().Contain(c =>
            c.GetProperty("type").GetString() == "text/plain" &&
            c.GetProperty("value").GetString() == "Hello plain text");
        content.Should().Contain(c =>
            c.GetProperty("type").GetString() == "text/html" &&
            c.GetProperty("value").GetString() == "<p>Hello HTML</p>");
    }

    [Fact]
    public async Task SendAsync_includes_inline_attachment_with_inline_disposition()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = SendGridBaseAddress };

        var sender = new SendGridSender(httpClient, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmailWithInlineAttachment();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var attachments = json.RootElement.GetProperty("attachments").EnumerateArray().ToList();

        attachments.Should().HaveCount(1);
        attachments[0].GetProperty("filename").GetString().Should().Be("logo.png");
        attachments[0].GetProperty("type").GetString().Should().Be("image/png");
        attachments[0].GetProperty("disposition").GetString().Should().Be("inline");
        attachments[0].GetProperty("content_id").GetString().Should().Be("logo@mailvolt");
        attachments[0].GetProperty("content").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendAsync_includes_attachment_with_attachment_disposition()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = SendGridBaseAddress };

        var sender = new SendGridSender(httpClient, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmailWithAttachment();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var attachments = json.RootElement.GetProperty("attachments").EnumerateArray().ToList();

        attachments.Should().HaveCount(1);
        attachments[0].GetProperty("filename").GetString().Should().Be("report.pdf");
        attachments[0].GetProperty("type").GetString().Should().Be("application/pdf");
        attachments[0].GetProperty("disposition").GetString().Should().Be("attachment");
    }

    [Fact]
    public async Task SendAsync_extracts_messageId_from_response_header()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Accepted);
        response.Headers.TryAddWithoutValidation("x-message-id", "sg-message-987");

        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler) { BaseAddress = SendGridBaseAddress };

        var sender = new SendGridSender(httpClient, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("sg-message-987");
    }

    [Fact]
    public async Task SendAsync_includes_categories_from_tags()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = SendGridBaseAddress };

        var sender = new SendGridSender(httpClient, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail() with { Tags = ["transactional", "receipt"] };

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var categories = json.RootElement.GetProperty("categories").EnumerateArray()
            .Select(c => c.GetString()).ToList();

        categories.Should().BeEquivalentTo(["transactional", "receipt"]);
    }

    [Fact]
    public async Task SendAsync_returns_failure_on_error_status()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("{\"errors\": [{\"message\": \"Forbidden\"}]}")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler) { BaseAddress = SendGridBaseAddress };

        var sender = new SendGridSender(httpClient, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("403");
    }

    [Fact]
    public async Task SendAsync_throws_OperationCanceledException_when_cancelled()
    {
        var handler = new Helpers.HttpMessageHandlerStub(_ =>
            throw new OperationCanceledException());
        var httpClient = new HttpClient(handler) { BaseAddress = SendGridBaseAddress };

        var sender = new SendGridSender(httpClient, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        Func<Task> act = () => sender.SendAsync(email);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
