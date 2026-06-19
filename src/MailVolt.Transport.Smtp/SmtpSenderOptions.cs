
using MailKit.Security;

namespace MailVolt.Transport.Smtp;

public sealed class SmtpSenderOptions
{
    public const string SectionName = "MailVolt:Smtp";
    public required string Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTlsWhenAvailable;
    public int TimeoutMs { get; set; } = 30_000;
    public Func<CancellationToken, Task<string>>? OAuth2TokenProvider { get; set; }
}
