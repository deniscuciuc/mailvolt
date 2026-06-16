namespace MailVolt.Templates.Razor;

/// <summary>
/// Options for configuring the Razor template renderer.
/// </summary>
public sealed class RazorTemplateOptions
{
    /// <summary>
    /// The root directory from which to resolve Razor views.
    /// Defaults to <see cref="Directory.GetCurrentDirectory()"/>.
    /// </summary>
    public string RootDirectory { get; set; } = Directory.GetCurrentDirectory();
}
