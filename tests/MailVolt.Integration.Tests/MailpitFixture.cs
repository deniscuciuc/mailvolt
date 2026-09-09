using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace MailVolt.Integration.Tests;

/// <summary>
/// Runs a Mailpit SMTP server in a container and exposes its HTTP API so tests can
/// assert on what was actually delivered over the wire. Mailpit matches what
/// <c>examples/docker-compose.yml</c> uses for local development.
/// </summary>
public sealed class MailpitFixture : IAsyncLifetime
{
    private const ushort SmtpPort = 1025;
    private const ushort HttpPort = 8025;

    private readonly IContainer _container = new ContainerBuilder("axllent/mailpit:v1.21")
        .WithPortBinding(SmtpPort, true)
        .WithPortBinding(HttpPort, true)
        .WithWaitStrategy(
            Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request =>
                request.ForPath("/readyz").ForPort(HttpPort)))
        .Build();

    private HttpClient? _api;

    public string Host => _container.Hostname;

    public ushort SmtpMappedPort => _container.GetMappedPublicPort(SmtpPort);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _api = new HttpClient
        {
            BaseAddress = new Uri(
                $"http://{_container.Hostname}:{_container.GetMappedPublicPort(HttpPort)}"),
        };
    }

    public async Task DisposeAsync()
    {
        _api?.Dispose();
        await _container.DisposeAsync();
    }

    /// <summary>Delete every captured message so each test starts from a clean mailbox.</summary>
    public async Task ClearAsync()
    {
        using var response = await Api.DeleteAsync(new Uri("/api/v1/messages", UriKind.Relative));
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Return the messages Mailpit has captured, newest first.</summary>
    public async Task<IReadOnlyList<MailpitMessageSummary>> GetMessagesAsync()
    {
        var list = await Api.GetFromJsonAsync<MailpitMessageList>(
            new Uri("/api/v1/messages", UriKind.Relative));

        return list?.Messages ?? [];
    }

    /// <summary>Return the full message, including bodies and attachment metadata.</summary>
    public async Task<MailpitMessage> GetMessageAsync(string id)
    {
        var message = await Api.GetFromJsonAsync<MailpitMessage>(
            new Uri($"/api/v1/message/{id}", UriKind.Relative));

        Assert.NotNull(message);
        return message;
    }

    /// <summary>Return the raw RFC 822 source, for asserting on headers MailVolt sets.</summary>
    public Task<string> GetRawAsync(string id) =>
        Api.GetStringAsync(new Uri($"/api/v1/message/{id}/raw", UriKind.Relative));

    private HttpClient Api =>
        _api ?? throw new InvalidOperationException("Fixture has not been initialized.");
}

public sealed class MailpitMessageList
{
    [JsonPropertyName("messages")]
    public List<MailpitMessageSummary> Messages { get; set; } = [];
}

public sealed class MailpitMessageSummary
{
    [JsonPropertyName("ID")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("To")]
    public List<MailpitAddress> To { get; set; } = [];
}

public sealed class MailpitMessage
{
    [JsonPropertyName("ID")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("From")]
    public MailpitAddress? From { get; set; }

    [JsonPropertyName("To")]
    public List<MailpitAddress> To { get; set; } = [];

    [JsonPropertyName("Cc")]
    public List<MailpitAddress> Cc { get; set; } = [];

    [JsonPropertyName("Bcc")]
    public List<MailpitAddress> Bcc { get; set; } = [];

    [JsonPropertyName("ReplyTo")]
    public List<MailpitAddress> ReplyTo { get; set; } = [];

    [JsonPropertyName("Text")]
    public string? Text { get; set; }

    [JsonPropertyName("HTML")]
    public string? Html { get; set; }

    [JsonPropertyName("Attachments")]
    public List<MailpitAttachment> Attachments { get; set; } = [];

    [JsonPropertyName("Inline")]
    public List<MailpitAttachment> Inline { get; set; } = [];
}

public sealed class MailpitAddress
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Address")]
    public string Address { get; set; } = string.Empty;
}

public sealed class MailpitAttachment
{
    [JsonPropertyName("FileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("ContentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("ContentID")]
    public string ContentId { get; set; } = string.Empty;

    [JsonPropertyName("Size")]
    public int Size { get; set; }
}
