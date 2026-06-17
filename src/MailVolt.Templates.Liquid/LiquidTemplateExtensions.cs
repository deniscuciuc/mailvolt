// ReSharper disable once CheckNamespace

using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MailVolt.Templates.Liquid.DependencyInjection;
/// <summary>
/// Extension methods for registering the Liquid template renderer.
/// </summary>
public static class LiquidTemplateExtensions
{
    /// <summary>
    /// Registers the Liquid template renderer as the <see cref="ITemplateRenderer"/> implementation.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> to add services to.</param>
    /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
    public static MailVoltBuilder UseLiquidTemplates(this MailVoltBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddTransient<ITemplateRenderer, LiquidTemplateRenderer>();
        return builder;
    }
}
