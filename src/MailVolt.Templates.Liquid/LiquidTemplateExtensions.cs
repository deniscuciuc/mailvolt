// ReSharper disable once CheckNamespace

using MailVolt.Templates.Liquid;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

// All MailVolt registration extensions live in MailVolt.Core.DependencyInjection, so one
// using covers AddMailVolt and every transport and template engine.
// ReSharper disable once CheckNamespace
namespace MailVolt.Core.DependencyInjection;
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
