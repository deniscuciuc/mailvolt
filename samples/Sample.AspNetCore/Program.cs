using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.Smtp.DependencyInjection;
using MailVolt.Templates.Razor.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register MailVolt with SMTP transport and Razor templates
builder.Services.AddMailVolt(builder.Configuration)
    .UseSmtpTransport(options =>
    {
        options.Host = builder.Configuration["MailVolt:Smtp:Host"] ?? "localhost";
        options.Port = int.Parse(builder.Configuration["MailVolt:Smtp:Port"] ?? "587");
        options.Username = builder.Configuration["MailVolt:Smtp:Username"];
        options.Password = builder.Configuration["MailVolt:Smtp:Password"];
    })
    .UseRazorTemplates();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();
