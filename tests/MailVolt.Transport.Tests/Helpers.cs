using MailVolt.Core.Models;

namespace MailVolt.Transport.Tests;

/// <summary>
/// Shared helper types for transport tests.
/// </summary>
internal static class Helpers
{
    /// <summary>
    /// Creates a mock <see cref="IOptions{T}"/> wrapper.
    /// </summary>
    public static IOptions<T> OptionsOf<T>(T value) where T : class
    {
        var opts = Substitute.For<IOptions<T>>();
        opts.Value.Returns(value);
        return opts;
    }

    /// <summary>
    /// A stub <see cref="HttpMessageHandler"/> that captures requests and returns fixed responses.
    /// </summary>
    internal sealed class HttpMessageHandlerStub : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _sendAsync;

        public HttpMessageHandlerStub(Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        /// <summary>Creates a handler that always returns the specified response.</summary>
        public static HttpMessageHandlerStub Returning(HttpResponseMessage response)
            => new(_ => Task.FromResult(response));

        /// <summary>Creates a handler that captures the request and returns a default OK response.</summary>
        public static HttpMessageHandlerStub Capture(Action<HttpRequestMessage> capture)
            => new(req =>
            {
                capture(req);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"test-id-123\"}")
                });
            });

        /// <summary>Creates a handler that captures the request and body content, returning a default OK response.</summary>
        public static HttpMessageHandlerStub Capture(Action<HttpRequestMessage, string> captureWithBody)
            => new(req =>
            {
                var body = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
                captureWithBody(req, body);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"test-id-123\"}")
                });
            });

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _sendAsync(request);
    }

    /// <summary>Creates a standard test email with common fields.</summary>
    public static EmailMessage CreateTestEmail()
        => new()
        {
            From = new EmailAddress("sender@example.com", "Sender"),
            To = [new EmailAddress("recipient@example.com", "Recipient")],
            Subject = "Test Subject",
            TextBody = "Hello plain text",
            HtmlBody = "<p>Hello HTML</p>"
        };

    /// <summary>Creates a test email with an inline attachment (content ID based).</summary>
    public static EmailMessage CreateTestEmailWithInlineAttachment()
        => new()
        {
            From = new EmailAddress("sender@example.com"),
            To = [new EmailAddress("recipient@example.com")],
            Subject = "Test with Inline",
            HtmlBody = "<img src=\"cid:logo@mailvolt\" />",
            Attachments =
            [
                new EmailAttachment
                {
                    FileName = "logo.png",
                    Content = new MemoryStream("image-data"u8.ToArray()),
                    ContentType = "image/png",
                    ContentId = "logo@mailvolt"
                }
            ]
        };

    /// <summary>Creates a test email with a regular (non-inline) attachment.</summary>
    public static EmailMessage CreateTestEmailWithAttachment()
        => new()
        {
            From = new EmailAddress("sender@example.com"),
            To = [new EmailAddress("recipient@example.com")],
            Subject = "Test with Attachment",
            TextBody = "See attached",
            Attachments =
            [
                new EmailAttachment
                {
                    FileName = "report.pdf",
                    Content = new MemoryStream("pdf-data"u8.ToArray()),
                    ContentType = "application/pdf"
                }
            ]
        };
}
