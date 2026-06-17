using Microsoft.Extensions.Options;
using Resend;

namespace MailVolt.Transport.Resend;

/// <summary>
/// Maps MailVolt <see cref="ResendSenderOptions"/> to the SDK's <see cref="ResendClientOptions"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ResendClientOptionsConfigure"/> class.
/// </remarks>
/// <param name="options">The MailVolt Resend options.</param>
internal sealed class ResendClientOptionsConfigure(IOptions<ResendSenderOptions> options) : IConfigureOptions<ResendClientOptions>
{
    private readonly ResendSenderOptions _options = options.Value;

    /// <inheritdoc />
    public void Configure(ResendClientOptions options)
    {
        options.ApiToken = _options.ApiKey;
        options.ApiUrl = _options.BaseUrl.TrimEnd('/');
    }
}
