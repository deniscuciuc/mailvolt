using AwesomeAssertions;
using MailKit.Security;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.AwsSes;
using MailVolt.Transport.AzureEmail;
using MailVolt.Transport.Brevo;
using MailVolt.Transport.Smtp;
using Xunit;

namespace MailVolt.Transport.Tests;

/// <summary>
/// Every transport must observe cancellation the same way, so swapping providers does not
/// change how a cancelled send behaves. Before this was enforced, SMTP, AWS SES and Azure
/// turned a cancellation into an ordinary <c>EmailResult.Failure</c> and Postmark returned
/// a failure with a "cancelled" message, while SendGrid, Resend, Brevo and Mailgun threw.
/// </summary>
public sealed class CancellationSemanticsTests
{
    public static TheoryData<string, Func<ISender>> Senders => new()
    {
        {
            "Smtp",
            () => new SmtpSender(Helpers.OptionsOf(new SmtpSenderOptions
            {
                Host = "127.0.0.1",
                Port = 1,
                Security = SecureSocketOptions.None,
            }))
        },
        {
            "AwsSes",
            () => new AwsSesSender(Helpers.OptionsOf(new AwsSesSenderOptions
            {
                AccessKeyId = "key",
                SecretAccessKey = "secret",
                Region = "us-east-1",
            }))
        },
        {
            "AzureEmail",
            () => new AzureEmailSender(Helpers.OptionsOf(new AzureEmailSenderOptions
            {
                ConnectionString = "endpoint=https://example.communication.azure.com/;accesskey=" +
                                   Convert.ToBase64String("not-a-real-key"u8.ToArray()),
            }))
        },
        {
            "Brevo",
            () => new BrevoSender(Helpers.OptionsOf(new BrevoSenderOptions { ApiKey = "key" }))
        },
    };

    [Theory]
    [MemberData(nameof(Senders))]
    public async Task An_already_cancelled_token_throws_rather_than_returning_a_failure(
        string name,
        Func<ISender> createSender)
    {
        var sender = createSender();
        var email = Helpers.CreateTestEmail();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => sender.SendAsync(email, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>(
            $"{name} must propagate cancellation like every other transport");

        (sender as IDisposable)?.Dispose();
    }
}
