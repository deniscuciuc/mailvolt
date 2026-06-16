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
        var actionContext = GetActionContext();

        // First try to resolve the view by name (FindView falls back to
        // search paths). If that fails, try an explicit application-relative path.
        var viewResult = _viewEngine.FindView(actionContext, template, isMainPage: false);
        if (!viewResult.Success)
        {
            viewResult = _viewEngine.GetView(
                _options.RootDirectory,
                template,
                isMainPage: false);
        }

        if (!viewResult.Success || viewResult.View is null)
        {
            throw new InvalidOperationException(
                $"Razor view '{template}' could not be found. " +
                $"Searched locations: {string.Join(", ", viewResult.SearchedLocations ?? [])}");
        }

        await using var writer = new StringWriter();
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

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
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
