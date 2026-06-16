using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;

/// <summary>
/// Example service that sends email as a side effect of business logic.
/// This is the code you want to test without hitting a real SMTP server.
/// </summary>
public sealed class UserService(IEmailBuilder emailBuilder)
{
    public async Task<bool> RegisterAsync(string email, string name, CancellationToken ct = default)
    {
        // ... save to DB ...

        var result = await emailBuilder
            .To(new EmailAddress(email, name))
            .Subject("Welcome!")
            .HtmlBody($"<h1>Hi {name}, welcome aboard!</h1>")
            .SendAsync(ct);

        return result.IsSuccess;
    }

    public async Task SendPasswordResetAsync(string email, string resetLink, CancellationToken ct = default)
    {
        await emailBuilder
            .To(email)
            .Subject("Reset your password")
            .Priority(EmailPriority.High)
            .HtmlBody($"<p>Click to reset: <a href='{resetLink}'>here</a></p>")
            .SendAsync(ct);
    }
}
