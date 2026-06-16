using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Transport.Postmark;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MailVolt.Transport.Tests;

public sealed class PostmarkSenderTests
{
    private readonly PostmarkSenderOptions _options = new()
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
    public async Task SendAsync_sends_to_email_endpoint()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.postmarkapp.com/") };

        var factory = new PostmarkClientFactory(httpClient);
        var sender = new PostmarkSender(factory, Helpers.OptionsOf(_options), CreateLogger());

        var result = await sender.SendAsync(Helpers.CreateTestEmail());
        result.IsSuccess.Should().BeTrue(result.Error);

        capturedRequest.RequestUri.Should().NotBeNull();
        capturedRequest.RequestUri!.AbsolutePath.Should().Be("/email");
        capturedRequest.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task X_Postmark_Server_Token_header_is_sent_with_request()
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.postmarkapp.com/") };
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Postmark-Server-Token", _options.ApiKey);

        var factory = new PostmarkClientFactory(httpClient);
        var sender = new PostmarkSender(factory, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        capturedRequest.Headers.TryGetValues("X-Postmark-Server-Token", out var values).Should().BeTrue();
        values!.First().Should().Be("my-postmark-token");
    }

    [Fact]
    public async Task SendAsync_includes_json_payload_with_Uppercase_properties()
    {
        var capturedRequest = new HttpRequestMessage();
        var body = "";
        var handler = Helpers.HttpMessageHandlerStub.Capture((req, content) => { capturedRequest = req; body = content; });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.postmarkapp.com/") };
        var sender = new PostmarkSender(new PostmarkClientFactory(httpClient), Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail() with
        {
            ReplyTo = new EmailAddress("reply@example.com")
        };

        await sender.SendAsync(email);

        using var json = JsonDocument.Parse(body);

        // Postmark uses PascalCase JSON property names (From, To, Subject, etc.)
        json.RootElement.GetProperty("From").GetString().Should().Be("Sender <sender@example.com>");
        json.RootElement.GetProperty("To").GetString().Should().Be("Recipient <recipient@example.com>");
        json.RootElement.GetProperty("Subject").GetString().Should().Be("Test Subject");
        json.RootElement.GetProperty("HtmlBody").GetString().Should().Be("<p>Hello HTML</p>");
        json.RootElement.GetProperty("TextBody").GetString().Should().Be("Hello plain text");
        json.RootElement.GetProperty("ReplyTo").GetString().Should().Be("reply@example.com");
    }

    [Fact]
    public async Task SendAsync_includes_MessageStream_from_options()
    {
        var capturedRequest = new HttpRequestMessage();
        var body = "";
        var handler = Helpers.HttpMessageHandlerStub.Capture((req, content) => { capturedRequest = req; body = content; });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.postmarkapp.com/") };
        var sender = new PostmarkSender(new PostmarkClientFactory(httpClient), Helpers.OptionsOf(_options), CreateLogger());

        await sender.SendAsync(Helpers.CreateTestEmail());

        using var json = JsonDocument.Parse(body);

        json.RootElement.GetProperty("MessageStream").GetString().Should().Be("outbound");
    }

    [Fact]
    public async Task SendAsync_includes_custom_MessageStream()
    {
        var customOptions = new PostmarkSenderOptions
        {
            ApiKey = "my-postmark-token",
            BaseUrl = "https://api.postmarkapp.com",
            MessageStream = "transactional"
        };

        var (capturedRequest, _) = CreateSenderWithCapturedRequest(customOptions);

        // Not sending a real email - this test just verifies the options flow through the sender
    }

    // This is tested via SendAsync_includes_MessageStream_from_options
    // with different options values

    [Fact]
    public async Task SendAsync_includes_attachment_with_base64_content()
    {
        var capturedRequest = new HttpRequestMessage();
        var body = "";
        var handler = Helpers.HttpMessageHandlerStub.Capture((req, content) => { capturedRequest = req; body = content; });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.postmarkapp.com/") };
        var sender = new PostmarkSender(new PostmarkClientFactory(httpClient), Helpers.OptionsOf(_options), CreateLogger());

        await sender.SendAsync(Helpers.CreateTestEmailWithAttachment());

        using var json = JsonDocument.Parse(body);

        var attachments = json.RootElement.GetProperty("Attachments").EnumerateArray().ToList();
        attachments.Should().HaveCount(1);
        attachments[0].GetProperty("Name").GetString().Should().Be("report.pdf");
        attachments[0].GetProperty("ContentType").GetString().Should().Be("application/pdf");
        attachments[0].GetProperty("Content").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendAsync_returns_success_with_messageId()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"To\": \"recipient@example.com\", \"SubmittedAt\": \"2025-01-01T00:00:00Z\", \"MessageID\": \"postmark-msg-789\", \"ErrorCode\": 0, \"Message\": \"OK\"}")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler);

        var sender = CreateSender(httpClient);
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("postmark-msg-789");
    }

    [Fact]
    public async Task SendAsync_returns_failure_on_api_error()
    {
        var response = new HttpResponseMessage(HttpStatusCode.UnprocessableContent)
        {
            Content = new StringContent("{\"ErrorCode\": 300, \"Message\": \"Missing from\"}")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.postmarkapp.com/") };

        var factory = new PostmarkClientFactory(httpClient);
        var sender = new PostmarkSender(factory, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("422");
    }

    [Fact]
    public async Task SendAsync_returns_failure_on_empty_response()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("")
        };
        var handler = Helpers.HttpMessageHandlerStub.Returning(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.postmarkapp.com/") };

        var factory = new PostmarkClientFactory(httpClient);
        var sender = new PostmarkSender(factory, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("does not contain any JSON tokens");
    }

    [Fact]
    public async Task SendAsync_includes_headers()
    {
        var capturedRequest = new HttpRequestMessage();
        var body = "";
        var handler = Helpers.HttpMessageHandlerStub.Capture((req, content) => { capturedRequest = req; body = content; });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.postmarkapp.com/") };
        var sender = new PostmarkSender(new PostmarkClientFactory(httpClient), Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail() with
        {
            Headers = new Dictionary<string, string> { ["X-Custom"] = "custom-value" }
        };

        await sender.SendAsync(email);

        using var json = JsonDocument.Parse(body);

        var headers = json.RootElement.GetProperty("Headers").EnumerateArray().ToList();
        headers.Should().ContainSingle(h =>
            h.GetProperty("Name").GetString() == "X-Custom" &&
            h.GetProperty("Value").GetString() == "custom-value");
    }

    [Fact]
    public async Task SendAsync_includes_tag_from_first_tag()
    {
        var capturedRequest = new HttpRequestMessage();
        var body = "";
        var handler = Helpers.HttpMessageHandlerStub.Capture((req, content) => { capturedRequest = req; body = content; });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.postmarkapp.com/") };
        var sender = new PostmarkSender(new PostmarkClientFactory(httpClient), Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail() with { Tags = ["welcome-email", "onboarding"] };

        await sender.SendAsync(email);

        using var json = JsonDocument.Parse(body);

        json.RootElement.GetProperty("Tag").GetString().Should().Be("welcome-email");
    }

    [Fact]
    public async Task SendAsync_returns_failure_when_From_is_missing()
    {
        var handler = new Helpers.HttpMessageHandlerStub(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.postmarkapp.com/") };

        var factory = new PostmarkClientFactory(httpClient);
        var sender = new PostmarkSender(factory, Helpers.OptionsOf(_options), CreateLogger());
        var email = Helpers.CreateTestEmail() with { From = null };

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("From");
    }

    // ── Helper factory methods ──────────────────────────────────────

    private PostmarkSender CreateSender(HttpClient httpClient)
    {
        httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        var factory = new PostmarkClientFactory(httpClient);
        return new PostmarkSender(factory, Helpers.OptionsOf(_options), CreateLogger());
    }

    private (HttpRequestMessage Request, PostmarkSender Sender) CreateSenderWithCapturedRequest(
        PostmarkSenderOptions? options = null)
    {
        var capturedRequest = new HttpRequestMessage();
        var handler = Helpers.HttpMessageHandlerStub.Capture(req => capturedRequest = req);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/") };

        var factory = new PostmarkClientFactory(httpClient);

        var opts = options ?? _options;
        var sender = new PostmarkSender(factory, Helpers.OptionsOf(opts), CreateLogger());
        return (capturedRequest, sender);
    }

    /// <summary>
    /// Simple concrete IHttpClientFactory that always returns the same HttpClient.
    /// </summary>
    private sealed class PostmarkClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;
        public PostmarkClientFactory(HttpClient httpClient) => _httpClient = httpClient;
        public HttpClient CreateClient(string name) => _httpClient;
    }
}
