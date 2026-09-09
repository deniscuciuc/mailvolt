// ReSharper disable once CheckNamespace


using MailVolt.Templates.Handlebars;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

// All MailVolt registration extensions live in MailVolt.Core.DependencyInjection, so one
// using covers AddMailVolt and every transport and template engine.
// ReSharper disable once CheckNamespace
namespace MailVolt.Core.DependencyInjection;
/// <summary>
/// Extension methods for registering the Handlebars template renderer.
/// </summary>
public static class HandlebarsTemplateExtensions
{
    /// <summary>
    /// Registers the Handlebars template renderer as the <see cref="ITemplateRenderer"/> implementation.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> to add services to.</param>
    /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
    public static MailVoltBuilder UseHandlebarsTemplates(this MailVoltBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddTransient<ITemplateRenderer, HandlebarsTemplateRenderer>();
        return builder;
    }
}
