// ReSharper disable once CheckNamespace

using MailVolt.Templates.Razor;
using System.Diagnostics;
using System.Reflection;
using MailVolt.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// All MailVolt registration extensions live in MailVolt.Core.DependencyInjection, so one
// using covers AddMailVolt and every transport and template engine.
// ReSharper disable once CheckNamespace
namespace MailVolt.Core.DependencyInjection;
/// <summary>
/// Extension methods for registering the Razor template renderer.
/// </summary>
public static class RazorTemplateExtensions
{
    /// <summary>
    /// Registers the Razor template renderer as the <see cref="ITemplateRenderer"/> implementation.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> to add services to.</param>
    /// <param name="configure">An optional delegate to configure <see cref="RazorTemplateOptions"/>.</param>
    /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
    public static MailVoltBuilder UseRazorTemplates(
        this MailVoltBuilder builder,
        Action<RazorTemplateOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var mvcBuilder = builder.Services.AddMvcCore().AddRazorViewEngine();
        builder.Services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationFormats.Add("{0}");
            options.AreaViewLocationFormats.Add("{0}");
        });

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
        {
            mvcBuilder.ConfigureApplicationPartManager(manager =>
            {
                if (!manager.ApplicationParts.OfType<AssemblyPart>().Any(p => p.Assembly == entryAssembly))
                {
                    manager.ApplicationParts.Add(new AssemblyPart(entryAssembly));
                }

                if (!manager.ApplicationParts.OfType<CompiledRazorAssemblyPart>().Any(p => p.Assembly == entryAssembly))
                {
                    manager.ApplicationParts.Add(new CompiledRazorAssemblyPart(entryAssembly));
                }
            });
        }

        builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        // Registered once and forwarded: adding the same instance under two descriptors
        // makes the container dispose it twice on shutdown. The container owns the
        // listener's lifetime from here.
#pragma warning disable CA2000 // Ownership transfers to the service provider.
        builder.Services.TryAddSingleton(new DiagnosticListener("Microsoft.AspNetCore"));
#pragma warning restore CA2000
        builder.Services.TryAddSingleton<DiagnosticSource>(
            sp => sp.GetRequiredService<DiagnosticListener>());
        builder.Services.AddTransient<ITemplateRenderer, RazorTemplateRenderer>();

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        return builder;
    }
}
