using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Transport.Smtp.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMailVolt(opts =>
    {
        opts.DefaultFromAddress = builder.Configuration["MailVolt:DefaultFromAddress"]!;
        opts.DefaultFromDisplayName = "MailVolt API Demo";
    })
    .UseSmtpTransport(builder.Configuration);

var app = builder.Build();

// POST /contact  { name, email, message }
app.MapPost("/contact", async (ContactRequest req, IEmailBuilder emailBuilder) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Message))
        return Results.BadRequest("email and message are required");

    var result = await emailBuilder
        .To(new EmailAddress("admin@example.com", "Site Admin"))
        .ReplyTo(new EmailAddress(req.Email, req.Name))
        .Subject($"📩 Contact form: {req.Name}")
        .HtmlBody($"""
            <h3>New contact form submission</h3>
            <p><strong>From:</strong> {req.Name} ({req.Email})</p>
            <p><strong>Message:</strong></p>
            <p>{req.Message}</p>
            """)
        .SendAsync();

    return result.IsSuccess
        ? Results.Ok(new { sent = true, messageId = result.MessageId })
        : Results.Problem(result.Error);
})
.WithName("SendContact");

// GET /health
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

app.Run();

public record ContactRequest(string Name, string Email, string Message);
