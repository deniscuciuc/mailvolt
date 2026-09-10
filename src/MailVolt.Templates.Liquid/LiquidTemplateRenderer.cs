using System.Collections.Concurrent;
using System.Globalization;
using Fluid;
using MailVolt.Core.Interfaces;

namespace MailVolt.Templates.Liquid;

/// <summary>
/// Renders Liquid templates using the Fluid parser and engine. Parsed templates are cached,
/// so a template used repeatedly is only parsed once.
/// </summary>
public sealed class LiquidTemplateRenderer : ITemplateRenderer
{
    /// <summary>
    /// Upper bound on distinct cached templates. Templates are usually a small fixed set,
    /// but a caller passing per-tenant or user-generated source would otherwise grow this
    /// without limit.
    /// </summary>
    private const int MaxCachedTemplates = 512;

    private static readonly FluidParser Parser = new();

    private static readonly ConcurrentDictionary<string, IFluidTemplate> Cache = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<string> RenderAsync<TModel>(
        string template,
        TModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        cancellationToken.ThrowIfCancellationRequested();

        var parsed = GetOrParse(template);

        var context = new TemplateContext(
            model ?? new object(),
            new TemplateOptions
            {
                CultureInfo = CultureInfo.InvariantCulture,
            });

        return parsed.RenderAsync(context).AsTask();
    }

    private static IFluidTemplate GetOrParse(string template)
    {
        if (Cache.TryGetValue(template, out var cached))
        {
            return cached;
        }

        if (!Parser.TryParse(template, out var parsed, out var error))
        {
            throw new InvalidOperationException($"Failed to parse Liquid template: {error}");
        }

        // Stop caching rather than evict: an unbounded cache is a slow leak, and past the
        // cap the far likelier explanation is dynamic template source, which should not be
        // retained at all.
        if (Cache.Count < MaxCachedTemplates)
        {
            Cache.TryAdd(template, parsed);
        }

        return parsed;
    }
}
