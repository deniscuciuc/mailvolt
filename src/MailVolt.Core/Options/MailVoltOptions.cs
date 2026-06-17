namespace MailVolt.Core.Options;

/// <summary>
/// Configuration options for the MailVolt library.
/// </summary>
public sealed class MailVoltOptions
{
    /// <summary>Default configuration section name.</summary>
    public const string SectionName = "MailVolt";

    /// <summary>
    /// The default sender email address used when <see cref="Models.EmailMessage.From"/> is not explicitly set.
    /// </summary>
    public string? DefaultFromAddress { get; set; }

    /// <summary>
    /// The default display name used alongside <see cref="DefaultFromAddress"/>.
    /// </summary>
    public string? DefaultFromDisplayName { get; set; }
}
