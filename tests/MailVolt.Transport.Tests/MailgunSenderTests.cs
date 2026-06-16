using System.Net;
using System.Text;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.Mailgun;
using Microsoft.Extensions.Options;
using Xunit;

namespace MailVolt.Transport.Tests;

public sealed class MailgunSenderTests
{
    private readonly MailgunSenderOptions _options = new()
    {
        ApiKey = "my-mailgun-api-key",
        Domain = "mg.example.com",
        BaseUrl = "https://api.mailgun.net/v3"
    };

    [Fact]
    public void MailgunSender_implements_IMailgunSender()
    {
        typeof(MailgunSender).Should().Implement<IMailgunSender>();
    }

    [Fact]
    public void IMailgunSender_extends_ISender()
    {
        typeof(IMailgunSender).Should().Implement<ISender>();
    }

    [Fact]
    public void Constructor_sets_auth_header()
    {
        var handler = new Helpers.HttpMessageHandlerStub(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var httpClient = new HttpClient(handler);

        _ = new MailgunSender(httpClient, Helpers.OptionsOf(_options));

        httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Basic");

        var expectedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes("api:" + _options.ApiKey));
        httpClient.DefaultRequestHeaders.Authorization.Parameter.Should().Be(expectedToken);
    }

    [Fact]
    public void Constructor_sets_base_address()
    {
        var handler = new Helpers.HttpMessageHandlerStub(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var httpClient = new HttpClient(handler);

        _ = new MailgunSender(httpClient, Helpers.OptionsOf(_options));

        httpClient.BaseAddress.Should().Be("https://api.mailgun.net/v3/");
    }

    [Fact]
    public async Task SendAsync_sends_to_correct_endpoint()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler);

        var sender = new MailgunSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        capturedRequest.RequestUri!.ToString().Should().Contain("mg.example.com/messages");
        capturedRequest.Method.Should().Be(HttpMethod.Post);
        capturedRequest.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public async Task SendAsync_includes_from_to_and_subject_in_form_data()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler);

        var sender = new MailgunSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();

        // Multipart form-data field names and values appear in the body
        body.Should().Contain("name=from");
        body.Should().Contain("Sender <sender@example.com>");
        body.Should().Contain("name=to");
        body.Should().Contain("Recipient <recipient@example.com>");
        body.Should().Contain("name=subject");
        body.Should().Contain("Test Subject");
    }

    [Fact]
    public async Task SendAsync_includes_text_and_html_body()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler);

        var sender = new MailgunSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();

        body.Should().Contain("name=text");
        body.Should().Contain("Hello plain text");
        body.Should().Contain("name=html");
        body.Should().Contain("<p>Hello HTML</p>");
    }

    [Fact]
    public async Task SendAsync_includes_inline_attachment_correctly()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler);

        var sender = new MailgunSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmailWithInlineAttachment();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();

        body.Should().Contain("inline");
        body.Should().Contain("logo.png");
        body.Should().Contain("<logo@mailvolt>");
    }

    [Fact]
    public async Task SendAsync_includes_regular_attachment()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler);

        var sender = new MailgunSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmailWithAttachment();

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();

        body.Should().Contain("attachment");
        body.Should().Contain("report.pdf");
        body.Should().Contain("application/pdf");
    }

    [Fact]
    public async Task SendAsync_returns_success_with_messageId()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\": \"<20250101.123456@mg.example.com>\", \"message\": \"Queued. Thank you.\"}")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler);

        var sender = new MailgunSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("<20250101.123456@mg.example.com>");
    }

    [Fact]
    public async Task SendAsync_includes_tags_in_form_data()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler);

        var sender = new MailgunSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail() with { Tags = ["notification", "welcome"] };

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();

        body.Should().Contain("name=\"o:tag\"");
        body.Should().Contain("notification");
        body.Should().Contain("welcome");
    }

    [Fact]
    public async Task SendAsync_returns_failure_on_api_error()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"message\": \"Unauthorized\"}")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler);

        var sender = new MailgunSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("401");
    }

    [Fact]
    public async Task SendAsync_includes_custom_headers_in_form_data()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler);

        var sender = new MailgunSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail() with
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Campaign-Id"] = "camp-123",
                ["X-User-Id"] = "user-456"
            }
        };

        await sender.SendAsync(email);

        var body = await capturedRequest.Content!.ReadAsStringAsync();

        body.Should().Contain("name=\"h:X-Campaign-Id\"");
        body.Should().Contain("camp-123");
        body.Should().Contain("name=\"h:X-User-Id\"");
        body.Should().Contain("user-456");
    }

    [Fact]
    public async Task SendAsync_returns_failure_when_response_missing_id()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\": \"Queued.\"}")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler);

        var sender = new MailgunSender(httpClient, Helpers.OptionsOf(_options));
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("message ID");
    }
}
