using System.Globalization;
using Fluid;
using MailVolt.Core.Interfaces;

namespace MailVolt.Templates.Liquid;

/// <summary>
/// Renders Liquid templates using the Fluid parser and engine.
/// </summary>
public sealed class LiquidTemplateRenderer : ITemplateRenderer
{
    private static readonly FluidParser Parser = new();

    /// <inheritdoc />
    public Task<string> RenderAsync<TModel>(
        string template,
        TModel model,
        CancellationToken cancellationToken = default)
    {
        if (!Parser.TryParse(template, out var parsed, out var error))
        {
            throw new InvalidOperationException(
                $"Failed to parse Liquid template: {error}");
        }

        var context = new TemplateContext(
            model ?? new object(),
            new TemplateOptions
            {
                CultureInfo = CultureInfo.InvariantCulture
            });

        return parsed.RenderAsync(context).AsTask();
    }
}
