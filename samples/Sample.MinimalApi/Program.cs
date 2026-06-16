using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.Resend.DependencyInjection;
using MailVolt.Templates.Liquid.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMailVolt()
    .UseResend(options =>
    {
        options.ApiKey = builder.Configuration["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend:ApiKey required");
    })
    .UseLiquidTemplates();

var app = builder.Build();

app.MapPost("/send", async (SendRequest request, IEmailBuilder email) =>
{
    var result = await email
        .From(request.From)
        .To(request.To)
        .Subject(request.Subject)
        .UsingTemplate("Hello {{ name }}, welcome to {{ product }}!", new { name = request.Name, product = "MailVolt" })
        .SendAsync();

    return result.IsSuccess ? Results.Ok(new { id = result.MessageId }) : Results.Problem(result.Error);
});

app.Run();

public sealed record SendRequest(string From, string To, string Subject, string Name);
