namespace MailVolt.Core.Interfaces;

/// <summary>
/// Abstraction for rendering templates into email body content.
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// Renders the specified template with the given model and returns the resulting string.
    /// </summary>
    /// <typeparam name="TModel">The type of the model data.</typeparam>
    /// <param name="template">The template content (e.g. Liquid, Handlebars, Razor).</param>
    /// <param name="model">The model object to bind to the template.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The rendered template output.</returns>
    Task<string> RenderAsync<TModel>(string template, TModel model, CancellationToken cancellationToken = default);
}
