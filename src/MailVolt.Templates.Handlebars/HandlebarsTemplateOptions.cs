using HandlebarsDotNet;

namespace MailVolt.Templates.Handlebars;

/// <summary>
/// Configuration for the Handlebars template renderer, including custom helpers and
/// partials.
/// </summary>
public sealed class HandlebarsTemplateOptions
{
    /// <summary>The default configuration section name.</summary>
    public const string SectionName = "MailVolt:Handlebars";

    private readonly Dictionary<string, HandlebarsHelper> _helpers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _partials = new(StringComparer.Ordinal);

    /// <summary>Helpers to register with the Handlebars engine, keyed by helper name.</summary>
    public IReadOnlyDictionary<string, HandlebarsHelper> Helpers => _helpers;

    /// <summary>Partial templates to register, keyed by partial name.</summary>
    public IReadOnlyDictionary<string, string> Partials => _partials;

    /// <summary>
    /// Registers a custom helper, usable as <c>{{ name arg }}</c> in a template.
    /// </summary>
    /// <param name="name">The helper name as it appears in templates.</param>
    /// <param name="helper">The helper implementation.</param>
    /// <returns>The same options instance, for chaining.</returns>
    public HandlebarsTemplateOptions RegisterHelper(string name, HandlebarsHelper helper)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(helper);

        _helpers[name] = helper;
        return this;
    }

    /// <summary>
    /// Registers a partial template, usable as <c>{{&gt; name }}</c> in a template.
    /// </summary>
    /// <param name="name">The partial name as it appears in templates.</param>
    /// <param name="template">The partial's Handlebars source.</param>
    /// <returns>The same options instance, for chaining.</returns>
    public HandlebarsTemplateOptions RegisterPartial(string name, string template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(template);

        _partials[name] = template;
        return this;
    }
}
