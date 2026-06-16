namespace MailVolt.Templates.Razor.DependencyInjection;

using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        builder.Services.AddMvcCore().AddRazorViewEngine();
        builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddTransient<ITemplateRenderer, RazorTemplateRenderer>();

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        return builder;
    }
}
