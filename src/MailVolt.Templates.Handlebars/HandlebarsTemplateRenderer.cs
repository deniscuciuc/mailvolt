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
    private static readonly ConcurrentDictionary<string, HandlebarsTemplate<object, string>> Cache = new();

    /// <inheritdoc />
    public Task<string> RenderAsync<TModel>(
        string templateKey,
        TModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateKey);

        var compiled = Cache.GetOrAdd(templateKey, key =>
        {
            var source = ResolveTemplateSource(key);
            return HandlebarsDotNet.Handlebars.Compile(source);
        });

        var result = compiled(model!);
        return Task.FromResult(result);
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
