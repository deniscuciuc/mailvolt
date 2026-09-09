using System.Collections.Concurrent;
using HandlebarsDotNet;
using MailVolt.Core.Interfaces;

namespace MailVolt.Templates.Handlebars;

/// <summary>
/// Renders Handlebars templates using the Handlebars.Net library.
/// Caches compiled templates in a concurrent dictionary.
/// </summary>
public sealed class HandlebarsTemplateRenderer : ITemplateRenderer
{
    /// <summary>
    /// Upper bound on distinct cached templates. The cache key is the file path or, for
    /// inline templates, the source itself — so a caller passing per-tenant or
    /// user-generated source would otherwise grow this without limit.
    /// </summary>
    private const int MaxCachedTemplates = 512;

    private static readonly ConcurrentDictionary<string, HandlebarsTemplate<object, string>> Cache =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<string> RenderAsync<TModel>(
        string template,
        TModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        cancellationToken.ThrowIfCancellationRequested();

        var compiled = GetOrCompile(template);

        return Task.FromResult(compiled(model!));
    }

    private static HandlebarsTemplate<object, string> GetOrCompile(string key)
    {
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var compiled = HandlebarsDotNet.Handlebars.Compile(ResolveTemplateSource(key));

        // Stop caching rather than evict: an unbounded cache is a slow leak, and past the
        // cap the far likelier explanation is dynamic template source.
        if (Cache.Count < MaxCachedTemplates)
        {
            Cache.TryAdd(key, compiled);
        }

        return compiled;
    }

    private static string ResolveTemplateSource(string key)
    {
        if (File.Exists(key))
        {
            return File.ReadAllText(key);
        }

        if (!Path.IsPathRooted(key))
        {
            var baseDirectoryPath = Path.Combine(AppContext.BaseDirectory, key);
            if (File.Exists(baseDirectoryPath))
            {
                return File.ReadAllText(baseDirectoryPath);
            }
        }

        return key;
    }
}
