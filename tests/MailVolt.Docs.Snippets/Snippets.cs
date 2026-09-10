// Compiles the code shown in README.md and the docs, so a snippet that cannot build fails
// here rather than in a reader's editor.
using System.Globalization;
using HandlebarsDotNet;
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Core.Transports;
using MailVolt.Templates.Razor;
using MailVolt.Testing;
using MailVolt.Transport.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DocSnippets;

// README: Quick start — explicit registration
public static class ReadmeRegistration
{
    public static void Register(IServiceCollection services)
    {
        services.AddMailVolt()
            .UseSmtpTransport(options =>
            {
                options.Host = "smtp.example.com";
                options.Username = "user";
                options.Password = "pass";
            });
    }
}

// README: sending
public sealed class WelcomeService(IEmailBuilder email)
{
    public async Task SendAsync(string recipient, CancellationToken cancellationToken)
    {
        var result = await email
            .From("sender@example.com")
            .To(recipient)
            .Subject("Hello from MailVolt!")
            .HtmlBody("<h1>Welcome!</h1>")
            .SendAsync(cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error);
        }
    }
}

// README: batch sending
public sealed class DigestService(IBatchEmailSender batchSender)
{
    public async Task<BatchEmailResult> SendAsync(
        IReadOnlyList<EmailMessage> emails,
        CancellationToken cancellationToken)
    {
        return await batchSender.SendBatchAsync(emails, new BatchSendOptions(
            MaxConcurrency: 10,
            DelayMs: 200,
            FailureStrategy: FailureStrategy.Continue), cancellationToken);
    }
}

// README + docs/advanced/testing.md: in-memory transport and assertions
public static class ReadmeTesting
{
    public static async Task AssertAsync()
    {
        var services = new ServiceCollection();
        services.AddMailVolt().UseInMemoryTransport();
        await using var provider = services.BuildServiceProvider();

        var sender = provider.GetRequiredService<InMemorySender>();

        await provider.GetRequiredService<IEmailBuilder>()
            .From("a@example.com").To("user@example.com").Subject("Welcome!")
            .TextBody("hi").SendAsync();

        sender.Should()
            .HaveCount(1)
            .ContainEmailTo("user@example.com")
            .ContainSubject("Welcome!");
    }
}

// docs/troubleshooting.md: checking whether the section bound
public static class Troubleshooting
{
    public static void CheckBinding(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<SmtpSenderOptions>>().Value;
        Console.WriteLine(options.Host);
    }

    public static void CorrectAndWrongBinding(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMailVolt().UseSmtpTransport(configuration);
        services.AddMailVolt().UseSmtpTransport(configuration.GetSection("MailVolt:Smtp"));
        services.AddMailVolt(configuration);
        services.AddMailVolt().UseSmtpTransport(configuration).UseRazorTemplates();
    }
}

// docs/troubleshooting.md: resolving a builder per send from a singleton
public sealed class Notifier(IServiceProvider services)
{
    public Task SendAsync(string to) =>
        services.GetRequiredService<IEmailBuilder>()
            .To(to)
            .Subject("Hello")
            .TextBody("Hi")
            .SendAsync();
}

// docs/templates/handlebars.md: helpers and partials
public static class HandlebarsSnippets
{
    public static void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMailVolt()
            .UseSmtpTransport(configuration)
            .UseHandlebarsTemplates(options =>
            {
                options.RegisterHelper("formatDate", (writer, _, parameters) =>
                {
                    var date = DateTime.Parse(parameters[0].ToString()!, CultureInfo.InvariantCulture);
                    writer.WriteSafeString(date.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture));
                });

                options.RegisterHelper("uppercase", (writer, _, parameters) =>
                    writer.WriteSafeString(parameters[0].ToString()!.ToUpperInvariant()));
            });

        services.AddMailVolt()
            .UseSmtpTransport(configuration)
            .UseHandlebarsTemplates(options =>
                options.RegisterPartial("footer", "<p>&copy; {{year}} Example Corp</p>"));
    }
}

// docs/migrating-from-fluentemail.md
public static class MigrationSnippets
{
    public static void Register(IServiceCollection services)
    {
        services
            .AddMailVolt(options =>
            {
                options.DefaultFromAddress = "noreply@example.com";
                options.DefaultFromDisplayName = "My App";
            })
            .UseRazorTemplates()
            .UseSmtpTransport(options =>
            {
                options.Host = "smtp.example.com";
                options.Port = 587;
            });

        services.AddMailVolt().UseRazorTemplates(options => options.RootDirectory = "Templates");
    }

    public static async Task SendAsync(IEmailBuilder email, CancellationToken cancellationToken)
    {
        var result = await email
            .To("user@example.com")
            .Subject("Welcome")
            .HtmlBody("<h1>Hi</h1>")
            .SendAsync(cancellationToken);

        if (result.IsFailure)
        {
            Console.WriteLine(result.Error + result.Exception);
        }

        await email
            .To("user@example.com")
            .Subject("Welcome")
            .UsingTemplate("Welcome", new { Name = "Ada" })
            .SendAsync(cancellationToken);
    }

    public static async Task BatchAsync(
        IEmailBuilder builder,
        IBatchEmailSender batchSender,
        IReadOnlyList<string> recipients,
        string body,
        CancellationToken cancellationToken)
    {
        var messages = new List<EmailMessage>();
        foreach (var recipient in recipients)
        {
            messages.Add(await builder.To(recipient).Subject("Digest").TextBody(body)
                .BuildAsync(cancellationToken));
        }

        await batchSender.SendBatchAsync(messages, new BatchSendOptions(
            MaxConcurrency: 10,
            DelayMs: 200,
            FailureStrategy: FailureStrategy.Continue), cancellationToken);
    }
}

// docs/advanced/attachments.md
public static class AttachmentSnippets
{
    public static async Task SendAsync(IEmailBuilder email, byte[] csvBytes, byte[] bytes, CancellationToken cancellationToken)
    {
        await email
            .To("user@example.com")
            .Subject("Your invoice")
            .TextBody("The invoice is attached.")
            .Attach(a => a.FromFile("invoices/2026-09.pdf"))
            .SendAsync(cancellationToken);

        await email
            .To("user@example.com").Subject("s").TextBody("b")
            .Attach(a => a
                .FromBytes("export", csvBytes)
                .WithFileName("export.csv")
                .WithContentType("text/csv"))
            .SendAsync(cancellationToken);

        await email
            .To("user@example.com")
            .Subject("Welcome")
            .HtmlBody("""
                <h1>Welcome</h1>
                <img src="cid:logo@mailvolt" alt="Our logo" />
                """)
            .Attach(a => a.FromFile("Assets/logo.png").AsInlineImage("logo@mailvolt"))
            .SendAsync(cancellationToken);

        await email
            .To("user@example.com").Subject("s").HtmlBody("<p>x</p>")
            .Attach(a => a
                .FromBytes("logo", bytes)
                .WithContentType("image/svg+xml")
                .AsInlineImage("logo@mailvolt"))
            .SendAsync(cancellationToken);

        await email
            .To("user@example.com")
            .Subject("Monthly report")
            .HtmlBody("""<img src="cid:logo@mailvolt" /><p>Report attached.</p>""")
            .Attach(a => a.FromFile("Assets/logo.png").AsInlineImage("logo@mailvolt"))
            .Attach(a => a.FromFile("reports/2026-09.pdf"))
            .SendAsync(cancellationToken);

        var message = await email.To("user@example.com").Subject("x").TextBody("y")
            .Attach(a => a.FromFile("report.pdf"))
            .BuildAsync(cancellationToken);

        using var stream = message.Attachments[0].OpenReadStream();
    }
}
