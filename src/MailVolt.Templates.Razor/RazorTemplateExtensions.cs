// ReSharper disable once CheckNamespace

using System.Diagnostics;
using System.Reflection;
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MailVolt.Templates.Razor.DependencyInjection;
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
        var diagnostics = new DiagnosticListener("Microsoft.AspNetCore");
        builder.Services.TryAddSingleton<DiagnosticSource>(diagnostics);
        builder.Services.TryAddSingleton<DiagnosticListener>(diagnostics);
        builder.Services.AddTransient<ITemplateRenderer, RazorTemplateRenderer>();

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        return builder;
    }
}
