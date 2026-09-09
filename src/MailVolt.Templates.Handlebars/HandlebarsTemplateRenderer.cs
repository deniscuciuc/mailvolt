using System.Collections.Concurrent;
using HandlebarsDotNet;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace MailVolt.Templates.Handlebars;

/// <summary>
/// Renders Handlebars templates using the Handlebars.Net library, caching compiled templates.
/// </summary>
public sealed class HandlebarsTemplateRenderer : ITemplateRenderer
{
    /// <summary>
    /// Upper bound on distinct cached templates. The cache key is the file path or, for
    /// inline templates, the source itself — so a caller passing per-tenant or
    /// user-generated source would otherwise grow this without limit.
    /// </summary>
    private const int MaxCachedTemplates = 512;

    private readonly ConcurrentDictionary<string, HandlebarsTemplate<object, string>> _cache =
        new(StringComparer.Ordinal);

    private readonly IHandlebars _handlebars;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlebarsTemplateRenderer"/> class.
    /// </summary>
    /// <param name="options">Helpers and partials to register with the engine.</param>
    public HandlebarsTemplateRenderer(IOptions<HandlebarsTemplateOptions>? options = null)
    {
        // An isolated environment rather than the global Handlebars instance, so registering
        // a helper here cannot leak into an unrelated consumer in the same process.
        _handlebars = HandlebarsDotNet.Handlebars.Create();

        var configuration = options?.Value;
        if (configuration is null)
        {
            return;
        }

        foreach (var (name, helper) in configuration.Helpers)
        {
            _handlebars.RegisterHelper(name, helper);
        }

        foreach (var (name, template) in configuration.Partials)
        {
            _handlebars.RegisterTemplate(name, template);
        }
    }

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

    private HandlebarsTemplate<object, string> GetOrCompile(string key)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var compiled = _handlebars.Compile(ResolveTemplateSource(key));

        // Stop caching rather than evict: an unbounded cache is a slow leak, and past the
        // cap the far likelier explanation is dynamic template source.
        if (_cache.Count < MaxCachedTemplates)
        {
            _cache.TryAdd(key, compiled);
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
