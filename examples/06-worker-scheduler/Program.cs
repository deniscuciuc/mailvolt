using MailVolt.Core.DependencyInjection;
using MailVolt.Testing.DependencyInjection;
using MailVolt.Templates.Liquid.DependencyInjection;
using MailVolt.Transport.Smtp.DependencyInjection;

var dryRun = args.Contains("--dry-run");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg => cfg
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables())
    .ConfigureServices((ctx, services) =>
    {
        var mv = services.AddMailVolt(opts =>
            opts.DefaultFromAddress = ctx.Configuration["MailVolt:DefaultFromAddress"]!);

        if (dryRun) mv.UseInMemoryTransport();
        else mv.UseSmtpTransport(ctx.Configuration);

        mv.UseLiquidTemplates();

        services.AddHostedService<DigestWorker>();
    })
    .Build();

await host.RunAsync();
