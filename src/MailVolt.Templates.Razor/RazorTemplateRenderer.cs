using MailVolt.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace MailVolt.Templates.Razor;

/// <summary>
/// Renders Razor views using the ASP.NET Core Razor view engine.
/// </summary>
public sealed class RazorTemplateRenderer : ITemplateRenderer
{
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly RazorTemplateOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RazorTemplateRenderer"/> class.
    /// </summary>
    /// <param name="viewEngine">The Razor view engine.</param>
    /// <param name="tempDataProvider">The temp data provider.</param>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <param name="options">The Razor template options.</param>
    public RazorTemplateRenderer(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider,
        IOptions<RazorTemplateOptions> options)
    {
        ArgumentNullException.ThrowIfNull(viewEngine);
        ArgumentNullException.ThrowIfNull(tempDataProvider);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<string> RenderAsync<TModel>(
        string template,
        TModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        cancellationToken.ThrowIfCancellationRequested();

        var actionContext = GetActionContext();

        // Resolve by view name first — FindView searches the configured view locations.
        var viewResult = _viewEngine.FindView(actionContext, template, isMainPage: false);

        if (!viewResult.Success)
        {
            // Then as a path, relative to RootDirectory if one is configured. Without this,
            // RootDirectory was a public, documented, configurable option that nothing read.
            // The FindView result is kept for the error message, since it carries the
            // searched locations.
            foreach (var candidate in CandidatePaths(template))
            {
                var byPath = _viewEngine.GetView(executingFilePath: null, candidate, isMainPage: false);
                if (byPath?.Success == true)
                {
                    viewResult = byPath;
                    break;
                }
            }
        }

        if (!viewResult.Success || viewResult.View is null)
        {
            var searched = viewResult.SearchedLocations ?? [];
            var root = _options.RootDirectory is { Length: > 0 } configured
                ? $" RootDirectory: '{configured}'."
                : string.Empty;

            throw new InvalidOperationException(
                $"Razor view '{template}' could not be found.{root} " +
                $"Searched locations: {string.Join(", ", searched)}");
        }

        var writer = new StringWriter();
        await using var writerScope = writer.ConfigureAwait(false);
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            new ViewDataDictionary<TModel>(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
            {
                Model = model
            },
            new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext).ConfigureAwait(false);
        return writer.ToString();
    }

    /// <summary>
    /// The paths to try for a template that did not resolve as a view name, in order.
    /// </summary>
    private IEnumerable<string> CandidatePaths(string template)
    {
        yield return template;

        if (Path.IsPathRooted(template) || _options.RootDirectory is not { Length: > 0 } root)
        {
            yield break;
        }

        yield return Path.Combine(root, template);

        // Razor's view engine treats a path without an extension as a view name, so a
        // RootDirectory-relative template usually needs the extension appended.
        if (!Path.HasExtension(template))
        {
            yield return Path.Combine(root, template + ".cshtml");
        }
    }

    private ActionContext GetActionContext()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };

        return new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
    }
}
